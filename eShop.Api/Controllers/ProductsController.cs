using AutoMapper;
using eShop.Core.DTOs;
using eShop.Core.Models;
using eShop.Core.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace eShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly IFileService fileService;
        private readonly IProductService productService;
        private readonly IVariantService variantService;

        public ProductsController(IUnitOfWork unitOfWork, IMapper mapper, IFileService fileService, IProductService productService, IVariantService variantService)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
            this.fileService = fileService;
            this.productService = productService;
            this.variantService = variantService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CreateProductDto createProductDto)
        {
            using var transaction = unitOfWork.BeginTransaction();
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingSku = await productService.ProductExistsBySKUAsync(createProductDto.SKU);
                if (existingSku)
                {
                    return BadRequest(new { message = "A product with this SKU already exists." });
                }

                var product = mapper.Map<Product>(createProductDto);
                var createdProduct = await unitOfWork.ProductRepository.AddAsync(product);
                await unitOfWork.SaveChangesAsync();

                List<string> uploadedImagePaths = new List<string>();
                if (createProductDto.Images != null && createProductDto.Images.Any())
                {
                    try
                    {
                        var imagePaths = await fileService.SaveFilesAsync(createProductDto.Images, "products");
                        uploadedImagePaths.AddRange(imagePaths);

                        for (int i = 0; i < imagePaths.Count; i++)
                        {
                            var image = new Image
                            {
                                ProductId = createdProduct.Id,
                                Url = imagePaths[i],
                                AltText = $"{createdProduct.Name} - Image {i + 1}",
                                IsPrimary = i == 0,
                                SortOrder = i
                            };

                            await unitOfWork.ImageRepository.AddAsync(image);
                        }

                        await unitOfWork.SaveChangesAsync();
                    }
                    catch (Exception fileEx)
                    {
                        foreach (var path in uploadedImagePaths)
                        {
                            await fileService.DeleteFileAsync(path);
                        }
                        throw new Exception($"Error uploading images: {fileEx.Message}", fileEx);
                    }
                }

                if (createProductDto.Variants != null && createProductDto.Variants.Any())
                {
                    try
                    {
                        foreach (var variantDto in createProductDto.Variants)
                        {
                            variantDto.ProductId = createdProduct.Id;
                            await variantService.CreateVariantAsync(variantDto);
                        }
                    }
                    catch (Exception variantEx)
                    {
                        foreach (var path in uploadedImagePaths)
                        {
                            await fileService.DeleteFileAsync(path);
                        }
                        throw new Exception($"Error creating variants: {variantEx.Message}", variantEx);
                    }
                }

                await unitOfWork.CommitTransactionAsync();
                var productDto = mapper.Map<ProductDTO>(createdProduct);
                return Ok(productDto);
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackTransactionAsync();
                return StatusCode(500, new { message = "An error occurred while creating the product.", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] bool? featured = null, [FromQuery] bool? active = null, [FromQuery] int? categoryId = null)
        {
            try
            {
                IEnumerable<CreateProductDto> products;

                if (featured.HasValue && featured.Value)
                {
                    products = await productService.GetFeaturedProductsAsync();
                }
                else if (active.HasValue && active.Value)
                {
                    products = await productService.GetActiveProductsAsync();
                }
                else if (categoryId.HasValue)
                {
                    products = await productService.GetProductsByCategoryAsync(categoryId.Value);
                }
                else
                {
                    products = await productService.GetAllProductsAsync();
                }

                return Ok(new { data = products, count = products.Count() });
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
                var product = await productService.GetProductByIdAsync(id);

                if (product == null)
                {
                    return NotFound(new { message = $"Product with ID {id} not found." });
                }

                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the product.", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateProductDto updateProductDto)
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

                // Handle image deletions
                if (updateProductDto.ImageIdsToDelete != null && updateProductDto.ImageIdsToDelete.Any())
                {
                    var imagesToRemove = existingProduct.Images
                        .Where(img => updateProductDto.ImageIdsToDelete.Contains(img.Id))
                        .ToList();

                    foreach (var image in imagesToRemove)
                    {
                        imagesToDelete.Add(image.Url);
                        unitOfWork.ImageRepository.Remove(image);
                    }
                }

                // Update product basic information
                var product = mapper.Map(updateProductDto, existingProduct);
                var updatedProduct = await unitOfWork.ProductRepository.UpdateAsync(product);
                await unitOfWork.SaveChangesAsync();

                // Handle new images
                List<string> uploadedImagePaths = new List<string>();
                if (updateProductDto.NewImages != null && updateProductDto.NewImages.Any())
                {
                    try
                    {
                        var imagePaths = await fileService.SaveFilesAsync(updateProductDto.NewImages, "products");
                        uploadedImagePaths.AddRange(imagePaths);

                        var existingImagesCount = existingProduct.Images.Count - (updateProductDto.ImageIdsToDelete?.Count ?? 0);

                        for (int i = 0; i < imagePaths.Count; i++)
                        {
                            var image = new Image
                            {
                                ProductId = id,
                                Url = imagePaths[i],
                                AltText = $"{updatedProduct.Name} - Image {existingImagesCount + i + 1}",
                                IsPrimary = existingImagesCount == 0 && i == 0,
                                SortOrder = existingImagesCount + i
                            };

                            await unitOfWork.ImageRepository.AddAsync(image);
                        }

                        await unitOfWork.SaveChangesAsync();
                    }
                    catch (Exception fileEx)
                    {
                        foreach (var path in uploadedImagePaths)
                        {
                            await fileService.DeleteFileAsync(path);
                        }
                        throw new Exception($"Error uploading new images: {fileEx.Message}", fileEx);
                    }
                }

                // Handle variants - FIXED LOGIC
                if (updateProductDto.Variants != null && updateProductDto.Variants.Any())
                {
                    try
                    {
                        // Get all existing variant IDs
                        var existingVariantIds = existingProduct.Variants.Select(v => v.Id).ToHashSet();
                        var processedVariantIds = new HashSet<int>();

                        foreach (var variantDto in updateProductDto.Variants)
                        {
                            if (variantDto.Id < 0)
                            {
                                // This is a variant marked for deletion (negative ID)
                                var variantIdToDelete = Math.Abs(variantDto.Id);
                                var deleted = await variantService.DeleteVariantAsync(variantIdToDelete);
                                if (!deleted)
                                {
                                    throw new Exception($"Failed to delete variant with ID {variantIdToDelete}");
                                }
                                processedVariantIds.Add(variantIdToDelete);
                            }
                            else if (variantDto.Id > 0)
                            {
                                // This is an existing variant to update
                                var updatedVariant = await variantService.UpdateVariantAsync(variantDto);
                                if (updatedVariant == null)
                                {
                                    throw new Exception($"Failed to update variant with ID {variantDto.Id}");
                                }
                                processedVariantIds.Add(variantDto.Id);
                            }
                            else
                            {
                                // This is a new variant (ID = 0)
                                var createVariantDto = mapper.Map<CreateVariantDTO>(variantDto);
                                createVariantDto.ProductId = id;
                                var newVariant = await variantService.CreateVariantAsync(createVariantDto);
                                if (newVariant == null)
                                {
                                    throw new Exception("Failed to create new variant");
                                }
                            }
                        }

                        // Delete any variants that weren't included in the update (removed from frontend)
                        var variantsToDelete = existingVariantIds.Except(processedVariantIds).ToList();
                        foreach (var variantId in variantsToDelete)
                        {
                            var deleted = await variantService.DeleteVariantAsync(variantId);
                            if (!deleted)
                            {
                                throw new Exception($"Failed to delete variant with ID {variantId}");
                            }
                        }
                    }
                    catch (Exception variantEx)
                    {
                        // Clean up uploaded images on variant error
                        foreach (var path in uploadedImagePaths)
                        {
                            await fileService.DeleteFileAsync(path);
                        }
                        throw new Exception($"Error processing variants: {variantEx.Message}", variantEx);
                    }
                }
                else
                {
                    // If no variants provided, delete all existing variants
                    var existingVariantIds = existingProduct.Variants.Select(v => v.Id).ToList();
                    foreach (var variantId in existingVariantIds)
                    {
                        await variantService.DeleteVariantAsync(variantId);
                    }
                }

                await unitOfWork.CommitTransactionAsync();

                // Clean up deleted images after successful transaction
                foreach (var imagePath in imagesToDelete)
                {
                    await fileService.DeleteFileAsync(imagePath);
                }

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
                var imagePaths = product.Images.Select(img => img.Url).ToList();

                var deleted = await productService.DeleteProductAsync(id);

                if (!deleted)
                {
                    return StatusCode(500, new { message = "Failed to delete the product." });
                }

                await unitOfWork.CommitTransactionAsync();

                foreach (var imagePath in imagePaths)
                {
                    await fileService.DeleteFileAsync(imagePath);
                }

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
    }
}