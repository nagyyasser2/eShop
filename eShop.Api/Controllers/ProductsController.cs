using eShop.Core.Services.Abstractions;
using eShop.Core.DTOs.Products;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using eShop.Core.Models;
using AutoMapper;

namespace eShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFileService fileService,
        IProductService productService,
        IProductCacheService cacheService) : ControllerBase
    {
        private const string PRODUCT_LIST_CACHE_PREFIX = "products:list:";
        private const string PRODUCT_DETAIL_CACHE_PREFIX = "products:detail:";

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductRequest createProductDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return Ok(await productService.CreateProductAsync(createProductDto));
        }

        [HttpGet]
        public async Task<IActionResult> Get(
        [FromQuery] string[]? tags,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? featured = null,
        [FromQuery] bool? active = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] int? daysBack = null) 
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;

                var cacheKey = GenerateListCacheKey(tags, page, pageSize, featured, active, categoryId, minPrice, maxPrice, daysBack);
                var cachedResult = await cacheService.GetAsync<object>(cacheKey);
                if (cachedResult != null)
                {
                    return Ok(cachedResult);
                }

                int skip = (page - 1) * pageSize;
                Expression<Func<Product, bool>> filter = p => true;

                if (featured.HasValue)
                {
                    Expression<Func<Product, bool>> featuredFilter = p => p.IsFeatured == featured.Value;
                    filter = CombineFilters(filter, featuredFilter);
                }

                if (active.HasValue)
                {
                    Expression<Func<Product, bool>> activeFilter = p => p.IsActive == active.Value;
                    filter = CombineFilters(filter, activeFilter);
                }

                if (categoryId.HasValue)
                {
                    Expression<Func<Product, bool>> categoryFilter = p => p.CategoryId == categoryId.Value;
                    filter = CombineFilters(filter, categoryFilter);
                }

                if (tags?.Length > 0)
                {
                    Expression<Func<Product, bool>> tagsFilter = p =>
                        tags.Any(tag => p.Tags != null &&
                                       (p.Tags == tag ||
                                        p.Tags.StartsWith(tag + ",") ||
                                        p.Tags.EndsWith("," + tag) ||
                                        p.Tags.Contains("," + tag + ",")));
                    filter = CombineFilters(filter, tagsFilter);
                }

                if (minPrice.HasValue || maxPrice.HasValue)
                {
                    Expression<Func<Product, bool>> priceFilter = p =>
                        (!minPrice.HasValue || p.Price >= minPrice.Value) &&
                        (!maxPrice.HasValue || p.Price <= maxPrice.Value);
                    filter = CombineFilters(filter, priceFilter);
                }

                if (daysBack.HasValue)
                {
                    var cutoffDate = DateTime.UtcNow.AddDays(-daysBack.Value);
                    Expression<Func<Product, bool>> dateFilter = p => p.CreatedAt >= cutoffDate;
                    filter = CombineFilters(filter, dateFilter);
                }

                var (products, totalCount) = await productService.GetFilteredPagedAsync(filter, skip, pageSize);
                var result = new { data = products, count = totalCount, page, pageSize };
                await cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving products.", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOneById(int id)
        {
            try
            {
                // Generate cache key
                var cacheKey = $"{PRODUCT_DETAIL_CACHE_PREFIX}{id}";

                // Try to get from cache
                //var cachedProduct = await cacheService.GetAsync<object>(cacheKey);
                //if (cachedProduct != null)
                //{
                //    return Ok(cachedProduct);
                //}

                // Cache miss - fetch from database
                var product = await productService.GetProductByIdAsync(id);

                if (product == null)
                {
                    return NotFound(new { message = $"Product with ID {id} not found." });
                }

                // Cache the product for 10 minutes
                //await cacheService.SetAsync(cacheKey, product, TimeSpan.FromMinutes(10));

                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the product.", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateProductRequest updateProductDto)
        {
            using var transaction = unitOfWork.BeginTransaction();
            try
            {
                if (id != updateProductDto.Id)
                {
                    return BadRequest(new { message = "Product ID mismatch." });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (!await ProductExists(id))
                {
                    return NotFound(new { message = $"Product with ID {id} not found." });
                }

                var existingSku = await productService.ProductExistsBySKUAsync(updateProductDto.SKU, id);
                if (existingSku)
                {
                    return BadRequest(new { message = "A product with this SKU already exists." });
                }

                var existingProduct = await unitOfWork.ProductRepository.GetByIdAsync(id, new[] { "Images", "Variants" });
                var imagesToDelete = new List<string>();

                //if (updateProductDto.ImageIdsToDelete != null && updateProductDto.ImageIdsToDelete.Any())
                //{
                //    var imagesToRemove = existingProduct.Images
                //        .Where(img => updateProductDto.ImageIdsToDelete.Contains(img.Id))
                //        .ToList();

                //    foreach (var image in imagesToRemove)
                //    {
                //        imagesToDelete.Add(image.Url);
                //        await _unitOfWork.ImageRepository.RemoveAsync(image);
                //    }
                //}

                var product = mapper.Map(updateProductDto, existingProduct);
                var updatedProduct = await unitOfWork.ProductRepository.UpdateAsync(product);
                await unitOfWork.SaveChangesAsync();

                //List<string> uploadedImagePaths = new List<string>();
                //if (updateProductDto.NewImages != null && updateProductDto.NewImages.Any())
                //{
                //    try
                //    {
                //        var imagePaths = await fileService.SaveFilesAsync(updateProductDto.NewImages, "products");
                //        uploadedImagePaths.AddRange(imagePaths);

                        //var existingImagesCount = existingProduct.Images.Count - (updateProductDto.ImageIdsToDelete?.Count ?? 0);

                        //for (int i = 0; i < imagePaths.Count; i++)
                        //{
                        //    var image = new Image
                        //    {
                        //        ProductId = id,
                        //        Url = imagePaths[i],
                        //        AltText = $"{updatedProduct.Name} - Image {existingImagesCount + i + 1}",
                        //        IsPrimary = existingImagesCount == 0 && i == 0,
                        //        SortOrder = existingImagesCount + i
                        //    };

                        //    await _unitOfWork.ImageRepository.AddAsync(image);
                        //}

                //        await unitOfWork.SaveChangesAsync();
                //    }
                //    catch (Exception fileEx)
                //    {
                //        foreach (var path in uploadedImagePaths)
                //        {
                //            await fileService.DeleteFileAsync(path);
                //        }
                //        throw new Exception($"Error uploading new images: {fileEx.Message}", fileEx);
                //    }
                //}

                //if (updateProductDto.Variants != null && updateProductDto.Variants.Any())
                //{
                //    try
                //    {
                //        var existingVariantIds = existingProduct.Variants.Select(v => v.Id).ToHashSet();
                //        var processedVariantIds = new HashSet<int>();

                //        foreach (var variantDto in updateProductDto.Variants)
                //        {
                //            if (variantDto.Id < 0)
                //            {
                //                var variantIdToDelete = Math.Abs(variantDto.Id);
                //                var deleted = await variantService.DeleteVariantAsync(variantIdToDelete);
                //                if (!deleted)
                //                {
                //                    throw new Exception($"Failed to delete variant with ID {variantIdToDelete}");
                //                }
                //                processedVariantIds.Add(variantIdToDelete);
                //            }
                //            else if (variantDto.Id > 0)
                //            {
                //                var updatedVariant = await variantService.UpdateVariantAsync(variantDto);
                //                if (updatedVariant == null)
                //                {
                //                    throw new Exception($"Failed to update variant with ID {variantDto.Id}");
                //                }
                //                processedVariantIds.Add(variantDto.Id);
                //            }
                //            else
                //            {
                //                var createVariantDto = mapper.Map<CreateVariantDTO>(variantDto);
                //                createVariantDto.ProductId = id;
                //                var newVariant = await variantService.CreateVariantAsync(createVariantDto);
                //                if (newVariant == null)
                //                {
                //                    throw new Exception("Failed to create new variant");
                //                }
                //            }
                //        }

                //        var variantsToDelete = existingVariantIds.Except(processedVariantIds).ToList();
                //        foreach (var variantId in variantsToDelete)
                //        {
                //            var deleted = await variantService.DeleteVariantAsync(variantId);
                //            if (!deleted)
                //            {
                //                throw new Exception($"Failed to delete variant with ID {variantId}");
                //            }
                //        }
                //    }
                //    catch (Exception variantEx)
                //    {
                //        foreach (var path in uploadedImagePaths)
                //        {
                //            await fileService.DeleteFileAsync(path);
                //        }
                //        throw new Exception($"Error processing variants: {variantEx.Message}", variantEx);
                //    }
                //}
                //else
                //{
                //    var existingVariantIds = existingProduct.Variants.Select(v => v.Id).ToList();
                //    foreach (var variantId in existingVariantIds)
                //    {
                //        await variantService.DeleteVariantAsync(variantId);
                //    }
                //}

                await unitOfWork.CommitTransactionAsync();

                foreach (var imagePath in imagesToDelete)
                {
                    await fileService.DeleteFileAsync(imagePath);
                }

                // Invalidate both detail and list caches
                await cacheService.InvalidateProductAsync(id);
                await cacheService.InvalidateProductListAsync();

                var finalProduct = await productService.GetProductByIdAsync(id);
                return Ok(finalProduct);
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackTransactionAsync();
                return StatusCode(500, new { message = "An error occurred while updating the product.", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var transaction = unitOfWork.BeginTransaction();
            try
            {
                if (!await ProductExists(id))
                {
                    return NotFound(new { message = $"Product with ID {id} not found." });
                }

                var product = await unitOfWork.ProductRepository.GetByIdAsync(id, new[] { "Images" });
                //var imagePaths = product.Images.Select(img => img.Url).ToList();

                var deleted = await productService.DeleteProductAsync(id);

                if (!deleted)
                {
                    return StatusCode(500, new { message = "Failed to delete the product." });
                }

                await unitOfWork.CommitTransactionAsync();

                //foreach (var imagePath in imagePaths)
                //{
                //    await _fileService.DeleteFileAsync(imagePath);
                //}

                // Invalidate both detail and list caches
                await cacheService.InvalidateProductAsync(id);
                await cacheService.InvalidateProductListAsync();

                return Ok(new { message = "Product deleted successfully." });
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackTransactionAsync();
                return StatusCode(500, new { message = "An error occurred while deleting the product.", error = ex.Message });
            }
        }

        private async Task<bool> ProductExists(int id)
        {
            return await productService.ProductExistsAsync(id);
        }

        private string GenerateListCacheKey(string[]? tags, int page, int pageSize,bool? featured, bool? active,int? categoryId,decimal? minPrice,decimal? maxPrice,int? daysBack) 
        {
            var keyParts = new List<string>
                                            {
                                                $"page:{page}",
                                                $"size:{pageSize}"
                                            };

            if (featured.HasValue) keyParts.Add($"featured:{featured.Value}");
            if (active.HasValue) keyParts.Add($"active:{active.Value}");
            if (categoryId.HasValue) keyParts.Add($"cat:{categoryId.Value}");
            if (minPrice.HasValue) keyParts.Add($"minp:{minPrice.Value}");
            if (maxPrice.HasValue) keyParts.Add($"maxp:{maxPrice.Value}");
            if (tags?.Length > 0) keyParts.Add($"tags:{string.Join("-", tags.OrderBy(t => t))}");
            if (daysBack.HasValue) keyParts.Add($"days:{daysBack.Value}"); // Add this line

            return $"{PRODUCT_LIST_CACHE_PREFIX}{string.Join(":", keyParts)}";
        }

        private Expression<Func<Product, bool>> CombineFilters(
            Expression<Func<Product, bool>> filter1,
            Expression<Func<Product, bool>> filter2)
        {
            var parameter = Expression.Parameter(typeof(Product), "p");
            var body = Expression.AndAlso(
                Expression.Invoke(filter1, parameter),
                Expression.Invoke(filter2, parameter));
            return Expression.Lambda<Func<Product, bool>>(body, parameter);
        }
    }
}