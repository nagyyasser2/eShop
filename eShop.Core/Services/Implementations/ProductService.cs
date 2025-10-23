using eShop.Core.Services.Abstractions;
using eShop.Core.DTOs.Products;
using System.Linq.Expressions;
using eShop.Core.Exceptions;
using eShop.Core.Models;
using System.Text;
using AutoMapper;

namespace eShop.Core.Services.Implementations
{
    public class ProductService(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IFileService fileService,
        CacheInvalidationHelper cacheInvalidation,
        IMapper mapper
    ) : IProductService
    {
        private const string ProductCachePrefix = "products:detail:";
        private const string ProductListCachePrefix = "products:list:";

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            string cacheKey = $"{ProductCachePrefix}{id}";

            var cachedProduct = await cacheService.GetAsync<ProductDto>(cacheKey);
            if (cachedProduct != null)
                return cachedProduct;

            string[] includes = ["ProductImages", "Category"];

            var product = await unitOfWork.ProductRepository.GetByIdAsync(id, includes);
            if (product == null)
                return null;

            var productDto = mapper.Map<ProductDto>(product);

            await cacheService.SetAsync(cacheKey, productDto, TimeSpan.FromMinutes(10));

            return productDto;
        }

        public async Task<(IEnumerable<ProductDto> Products, int TotalCount)> GetFilteredPagedAsync(
            int skip,
            int take,
            bool? featured = null,
            bool? active = null,
            int? categoryId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? daysBack = null,
            string[]? tags = null,
            string[]? includes = null)
        {
            includes ??= ["Category", "ProductImages"];

            string cacheKey = GenerateCacheKey(skip, take, featured, active, categoryId, minPrice, maxPrice, daysBack, tags);

            var cachedResult = await cacheService.GetAsync<(IEnumerable<ProductDto> Products, int TotalCount)>(cacheKey);
            if (cachedResult.Products != null && cachedResult.Products.Any())
                return cachedResult;

            // Build the filter expression
            Expression<Func<Product, bool>> filter = BuildFilter(featured, active, categoryId, minPrice, maxPrice, daysBack, tags);

            var products = await unitOfWork.ProductRepository.GetFilteredPagedAsync(filter, skip, take, includes);
            var totalCount = await unitOfWork.ProductRepository.CountAsync(filter);
            var productDTOs = mapper.Map<IEnumerable<ProductDto>>(products);

            await cacheService.SetAsync(cacheKey, (productDTOs, totalCount), TimeSpan.FromMinutes(5));

            return (productDTOs, totalCount);
        }

        private string GenerateCacheKey(
            int skip,
            int take,
            bool? featured,
            bool? active,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            int? daysBack,
            string[]? tags)
        {
            var keyBuilder = new StringBuilder($"{ProductListCachePrefix}{skip}-{take}");

            if (featured.HasValue)
                keyBuilder.Append($":f={featured.Value}");

            if (active.HasValue)
                keyBuilder.Append($":a={active.Value}");

            if (categoryId.HasValue)
                keyBuilder.Append($":c={categoryId.Value}");

            if (minPrice.HasValue)
                keyBuilder.Append($":minp={minPrice.Value}");

            if (maxPrice.HasValue)
                keyBuilder.Append($":maxp={maxPrice.Value}");

            if (daysBack.HasValue)
                keyBuilder.Append($":d={daysBack.Value}");

            if (tags != null && tags.Length > 0)
            {
                var sortedTags = string.Join(",", tags.OrderBy(t => t));
                keyBuilder.Append($":t={sortedTags}");
            }

            return keyBuilder.ToString();
        }

        private Expression<Func<Product, bool>> BuildFilter(
            bool? featured,
            bool? active,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            int? daysBack,
            string[]? tags)
        {
            Expression<Func<Product, bool>> filter = p => true;

            if (featured.HasValue)
                filter = CombineExpressions(filter, p => p.IsFeatured == featured.Value);

            if (active.HasValue)
                filter = CombineExpressions(filter, p => p.IsActive == active.Value);

            if (categoryId.HasValue)
                filter = CombineExpressions(filter, p => p.CategoryId == categoryId.Value);

            if (minPrice.HasValue)
                filter = CombineExpressions(filter, p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                filter = CombineExpressions(filter, p => p.Price <= maxPrice.Value);

            if (daysBack.HasValue)
            {
                var sinceDate = DateTime.UtcNow.AddDays(-daysBack.Value);
                filter = CombineExpressions(filter, p => p.CreatedAt >= sinceDate);
            }

            if (tags != null && tags.Length > 0)
            {
                filter = CombineExpressions(filter, p => tags.Any(tag => p.Tags.Contains(tag)));
            }

            return filter;
        }

        private Expression<Func<T, bool>> CombineExpressions<T>(
            Expression<Func<T, bool>> expr1,
            Expression<Func<T, bool>> expr2)
        {
            var parameter = Expression.Parameter(typeof(T));
            var leftVisitor = new ReplaceExpressionVisitor(expr1.Parameters[0], parameter);
            var left = leftVisitor.Visit(expr1.Body);
            var rightVisitor = new ReplaceExpressionVisitor(expr2.Parameters[0], parameter);
            var right = rightVisitor.Visit(expr2.Body);
            var body = Expression.AndAlso(left!, right!);
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        private class ReplaceExpressionVisitor : ExpressionVisitor
        {
            private readonly Expression _oldValue;
            private readonly Expression _newValue;

            public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
            {
                _oldValue = oldValue;
                _newValue = newValue;
            }

            public override Expression? Visit(Expression? node)
                => node == _oldValue ? _newValue : base.Visit(node);
        }

        public async Task<ProductDto?> CreateProductAsync(CreateProductRequest createProductRequest)
        {
            try
            {
                var productImages = await SaveProductFilesAsync(createProductRequest.ProductImages);

                using var transaction = unitOfWork.BeginTransaction();
                try
                {
                    var product = mapper.Map<Product>(createProductRequest);
                    product.ProductImages = productImages;

                    await unitOfWork.ProductRepository.AddAsync(product);
                    await unitOfWork.SaveChangesAsync();
                    await unitOfWork.CommitTransactionAsync();

                    var createdProduct = mapper.Map<ProductDto>(product);

                    await cacheInvalidation.InvalidateProductListsAsync();

                    return createdProduct;
                }
                catch (Exception ex)
                {
                    await unitOfWork.RollbackTransactionAsync();
                    throw new ApplicationException("Failed to create product in database. Transaction rolled back.", ex);
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to create product. Files cleaned up.", ex);
            }
        }

        public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductRequest updateProductRequest)
        {
            var product = await GetProductWithImagesAsync(id);
            var currentProductImages = product.ProductImages.ToList();

            var (imagesToDelete, imagesToAdd) = ClassifyProductImages(updateProductRequest);

            await unitOfWork.BeginTransactionAsync();

            try
            {
                await DeleteProductImagesAsync(imagesToDelete, currentProductImages);
                await AddProductImagesAsync(imagesToAdd, currentProductImages, product.Id);

                mapper.Map(updateProductRequest, product);
                product.ProductImages = currentProductImages;

                await unitOfWork.ProductRepository.UpdateAsync(product);
                await unitOfWork.SaveChangesAsync();
                await unitOfWork.CommitTransactionAsync();

                await cacheInvalidation.InvalidateProductAsync(id);

                return mapper.Map<ProductDto>(product);
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackTransactionAsync();
                throw new Exception($"Failed to update product with ID {id}: {ex.Message}", ex);
            }
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await unitOfWork.ProductRepository.GetByIdAsync(id, new[] { "ProductImages" });
            if (product == null)
                throw new NotFoundException($"Product with ID {id} not found.");

            foreach (var productImage in product.ProductImages)
                await fileService.DeleteFileAsync(productImage.Path);

            unitOfWork.ProductRepository.Remove(product);
            await unitOfWork.SaveChangesAsync();

            await cacheInvalidation.InvalidateProductAsync(id);
        }

        private async Task<List<ProductImage>> SaveProductFilesAsync(ICollection<CreateProductImageRequest> productImages)
        {
            var productImageList = new List<ProductImage>();
            var savedFilePaths = new List<string>();

            foreach (var productImage in productImages)
            {
                try
                {
                    var path = await fileService.SaveFileAsync(productImage.File, "products");
                    savedFilePaths.Add(path);

                    productImageList.Add(new ProductImage
                    {
                        Path = path,
                        IsPrimary = productImage.IsPrimary
                    });
                }
                catch (Exception ex)
                {
                    await CleanupFilesAsync(savedFilePaths);
                    throw new ApplicationException($"Failed to save product image file: {productImage.File.FileName}", ex);
                }
            }

            return productImageList;
        }

        private async Task CleanupFilesAsync(List<string> filePaths)
        {
            foreach (var filePath in filePaths)
            {
                try
                {
                    await fileService.DeleteFileAsync(filePath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to delete file {filePath}: {ex.Message}");
                }
            }
        }

        private async Task<Product> GetProductWithImagesAsync(int id)
        {
            return await unitOfWork.ProductRepository.GetByIdAsync(id, ["ProductImages"])
                ?? throw new NotFoundException($"Product with ID {id} not found.");
        }

        private (List<DeleteProductImageDto> imagesToDelete, List<CreateProductImageDto> imagesToAdd)
            ClassifyProductImages(UpdateProductRequest updateRequest)
        {
            var toDelete = new List<DeleteProductImageDto>();
            var toAdd = new List<CreateProductImageDto>();

            foreach (var item in updateRequest.ProductImages)
            {
                if (item.IsDeletable)
                    toDelete.Add(new DeleteProductImageDto { Id = item.Id, Path = item.Path });

                if (item.File != null)
                    toAdd.Add(new CreateProductImageDto { File = item.File, IsPrimary = item.IsPrimary });
            }

            return (toDelete, toAdd);
        }

        private async Task DeleteProductImagesAsync(List<DeleteProductImageDto> imagesToDelete, List<ProductImage> currentImages)
        {
            foreach (var item in imagesToDelete)
            {
                await fileService.DeleteFileAsync(item.Path);

                var imageToRemove = currentImages.FirstOrDefault(i => i.Id == item.Id);
                if (imageToRemove != null)
                {
                    currentImages.Remove(imageToRemove);
                    await unitOfWork.ProductImageRepository.RemoveAsync(imageToRemove);
                }
            }
        }

        private async Task AddProductImagesAsync(List<CreateProductImageDto> imagesToAdd, List<ProductImage> currentImages, int productId)
        {
            foreach (var item in imagesToAdd)
            {
                var path = await fileService.SaveFileAsync(item.File, "products");
                var newImage = new ProductImage
                {
                    Path = path,
                    ProductId = productId,
                    IsPrimary = item.IsPrimary
                };
                currentImages.Add(newImage);
            }
        }

        public async Task<bool> ToggleProductStatusAsync(int id)
        {
            var product = await unitOfWork.ProductRepository.GetByIdAsync(id);
            if (product == null) return false;

            product.IsActive = !product.IsActive;
            unitOfWork.ProductRepository.Update(product);
            await unitOfWork.SaveChangesAsync();

            await cacheInvalidation.InvalidateProductAsync(id);

            return true;
        }

        public async Task<bool> ToggleFeaturedStatusAsync(int id)
        {
            var product = await unitOfWork.ProductRepository.GetByIdAsync(id);
            if (product == null) return false;

            product.IsFeatured = !product.IsFeatured;
            unitOfWork.ProductRepository.Update(product);
            await unitOfWork.SaveChangesAsync();

            await cacheInvalidation.InvalidateProductAsync(id);

            return true;
        }

        public async Task<bool> UpdateStockQuantityAsync(int id, int quantity)
        {
            var product = await unitOfWork.ProductRepository.GetByIdAsync(id);
            if (product == null) return false;

            product.StockQuantity = quantity;
            unitOfWork.ProductRepository.Update(product);
            await unitOfWork.SaveChangesAsync();

            await cacheService.InvalidateAsync($"{ProductCachePrefix}{id}");

            return true;
        }

        public async Task<bool> ProductExistsAsync(int id)
        {
            var product = await unitOfWork.ProductRepository.GetByIdAsync(id);
            return product != null;
        }

        public async Task<bool> ProductExistsBySKUAsync(string sku, int? excludeProductId = null)
        {
            var products = await unitOfWork.ProductRepository.FindAllAsync(p => p.Sku == sku);
            if (excludeProductId.HasValue)
                products = products.Where(p => p.Id != excludeProductId.Value);
            return products.Any();
        }
    }
}