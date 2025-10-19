using eShop.Core.Services.Abstractions;
using eShop.Core.DTOs.Products;
using System.Linq.Expressions;
using eShop.Core.Exceptions;
using eShop.Core.Models;
using AutoMapper;

namespace eShop.Core.Services.Implementations
{
    public class ProductService(IUnitOfWork unitOfWork, IFileService fileService, IMapper mapper) : IProductService
    {
        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            string[] includes = ["ProductImages", "Category" ];

            var product = await unitOfWork.ProductRepository.GetByIdAsync(id, includes);

            if (product == null)
            {
                return null;
            }

            return mapper.Map<ProductDto>(product);
        }

        public async Task<(IEnumerable<ProductDto> Products, int TotalCount)> GetFilteredPagedAsync(
            Expression<Func<Product, bool>> filter,
            int skip,
            int take,
            string[]? includes = null)
        {
            includes ??= new[] { "Category", "ProductImages", "Variants" };

            var products = await unitOfWork.ProductRepository.GetFilteredPagedAsync(filter, skip, take, includes);

            var totalCount = await unitOfWork.ProductRepository.CountAsync(filter);

            var productDTOs = mapper.Map<IEnumerable<ProductDto>>(products);

            return (productDTOs, totalCount);
        }

        public async Task<ProductDto?> CreateProductAsync(CreateProductRequest createProductRequest)
        {
            try
            {
                var productImages = await SaveProductFilesAsync(createProductRequest.ProductImages);
                using (var transaction = unitOfWork.BeginTransaction())
                {
                    try
                    {
                        var product = mapper.Map<Product>(createProductRequest);
                        product.ProductImages = productImages;

                        await unitOfWork.ProductRepository.AddAsync(product);
                        await unitOfWork.SaveChangesAsync();
                        await unitOfWork.CommitTransactionAsync();

                        return mapper.Map<ProductDto>(product);
                    }
                    catch (Exception ex)
                    {
                        await unitOfWork.RollbackTransactionAsync();
                        throw new ApplicationException("Failed to create product in database. Transaction rolled back.", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to create product. Files cleaned up.", ex);
            }
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

                var result  = await unitOfWork.ProductRepository.UpdateAsync(product);
                await unitOfWork.SaveChangesAsync();

                await unitOfWork.CommitTransactionAsync();

                return mapper.Map<ProductDto>(product);
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackTransactionAsync();

                throw new Exception($"Failed to update product with ID {id}: {ex.Message}", ex);
            }
        }

        private async Task<Product> GetProductWithImagesAsync(int id)
        {
            return await unitOfWork.ProductRepository.GetByIdAsync(id, ["ProductImages"])
                ?? throw new NotFoundException($"Product with ID {id} not found.");
        }

        private (List<DeleteProductImageDto> imagesToDelete, List<CreateProductImageDto> imagesToAdd) ClassifyProductImages(UpdateProductRequest updateRequest)
        {
            var toDelete = new List<DeleteProductImageDto>();
            var toAdd = new List<CreateProductImageDto>();

            foreach (var item in updateRequest.ProductImages)
            {
                if (item.IsDeletable)
                {
                    toDelete.Add(new DeleteProductImageDto
                    {
                        Id = item.Id,
                        Path = item.Path
                    });
                }

                if (item.File != null)
                {
                    toAdd.Add(new CreateProductImageDto
                    {
                        File = item.File,
                        IsPrimary = item.IsPrimary
                    });
                }
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

        public async Task DeleteProductAsync(int id)
        {
            var product = await unitOfWork.ProductRepository.GetByIdAsync(id, new[] { "ProductImages" });

            if (product == null)
                throw new NotFoundException($"Product with ID {id} not found.");

            foreach (var productImage in product.ProductImages)
            {
                await fileService.DeleteFileAsync(productImage.Path);
            }

            unitOfWork.ProductRepository.Remove(product);
            await unitOfWork.SaveChangesAsync();
        }

        public async Task<bool> ToggleProductStatusAsync(int id)
        {
            var product = await unitOfWork.ProductRepository.GetByIdAsync(id);
            if (product == null) return false;

            product.IsActive = !product.IsActive;
            unitOfWork.ProductRepository.Update(product);
            await unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleFeaturedStatusAsync(int id)
        {
            var product = await unitOfWork.ProductRepository.GetByIdAsync(id);
            if (product == null) return false;

            product.IsFeatured = !product.IsFeatured;
            unitOfWork.ProductRepository.Update(product);
            await unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStockQuantityAsync(int id, int quantity)
        {
            var product = await unitOfWork.ProductRepository.GetByIdAsync(id);
            if (product == null) return false;

            product.StockQuantity = quantity;
            unitOfWork.ProductRepository.Update(product);
            await unitOfWork.SaveChangesAsync();
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
            {
                products = products.Where(p => p.Id != excludeProductId.Value);
            }
            return products.Any();
        }
    }
}
