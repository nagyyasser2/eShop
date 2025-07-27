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
                // Validate the model
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if SKU already exists
                var existingSku = await productService.ProductExistsBySKUAsync(createProductDto.SKU);
                if (existingSku)
                {
                    return BadRequest(new { message = "A product with this SKU already exists." });
                }

                // Create the product
                var product = mapper.Map<Product>(createProductDto);
                var createdProduct = await unitOfWork.ProductRepository.AddAsync(product);
                await unitOfWork.SaveChangesAsync();

                // Handle image uploads if provided
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
                        // Clean up uploaded files on error
                        foreach (var path in uploadedImagePaths)
                        {
                            await fileService.DeleteFileAsync(path);
                        }
                        throw new Exception($"Error uploading images: {fileEx.Message}", fileEx);
                    }
                }

                //// Handle variant creation if provided
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
                        // Clean up uploaded files on error
                        foreach (var path in uploadedImagePaths)
                        {
                            await fileService.DeleteFileAsync(path);
                        }
                        throw new Exception($"Error creating variants: {variantEx.Message}", variantEx);
                    }
                }

                // Commit transaction
                await unitOfWork.CommitTransactionAsync();

                return Ok(product);
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

                // Check if product exists
                if (!await ProductExists(id))
                {
                    return NotFound(new { message = $"Product with ID {id} not found." });
                }

                // Check if SKU already exists for another product
                var existingSku = await productService.ProductExistsBySKUAsync(updateProductDto.SKU, id);
                if (existingSku)
                {
                    return BadRequest(new { message = "A product with this SKU already exists." });
                }

                // Get existing product with images for cleanup
                var existingProduct = await unitOfWork.ProductRepository.GetByIdAsync(id, new[] { "Images" });
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
                var productUpdateDto = mapper.Map<CreateProductDto>(updateProductDto);
                var updatedProduct = await productService.UpdateProductAsync(productUpdateDto);

                if (updatedProduct == null)
                {
                    return NotFound(new { message = $"Product with ID {id} not found." });
                }

                // Handle new image uploads
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
                                IsPrimary = existingImagesCount == 0 && i == 0, // Set as primary if no existing images
                                SortOrder = existingImagesCount + i
                            };

                            await unitOfWork.ImageRepository.AddAsync(image);
                        }

                        await unitOfWork.SaveChangesAsync();
                    }
                    catch (Exception fileEx)
                    {
                        // Clean up uploaded files on error
                        foreach (var path in uploadedImagePaths)
                        {
                            await fileService.DeleteFileAsync(path);
                        }
                        throw new Exception($"Error uploading new images: {fileEx.Message}", fileEx);
                    }
                }

                // Commit transaction
                await unitOfWork.CommitTransactionAsync();

                // Clean up deleted image files after successful transaction
                foreach (var imagePath in imagesToDelete)
                {
                    await fileService.DeleteFileAsync(imagePath);
                }

                // Return updated product
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

                // Get product with all related images before deletion
                var product = await unitOfWork.ProductRepository.GetByIdAsync(id, new[] { "Images" });
                var imagePaths = product.Images.Select(img => img.Url).ToList();

                // Delete the product (this will cascade delete images due to foreign key)
                var deleted = await productService.DeleteProductAsync(id);

                if (!deleted)
                {
                    return StatusCode(500, new { message = "Failed to delete the product." });
                }

                // Commit transaction
                await unitOfWork.CommitTransactionAsync();

                // Clean up image files after successful deletion
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

        [HttpPatch("{id}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            using var transaction = unitOfWork.BeginTransaction();
            try
            {
                if (!await ProductExists(id))
                {
                    return NotFound(new { message = $"Product with ID {id} not found." });
                }

                var result = await productService.ToggleProductStatusAsync(id);

                if (!result)
                {
                    return StatusCode(500, new { message = "Failed to toggle product status." });
                }

                await unitOfWork.CommitTransactionAsync();
                return Ok(new { message = "Product status toggled successfully." });
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackTransactionAsync();
                return StatusCode(500, new { message = "An error occurred while toggling product status.", error = ex.Message });
            }
        }

        [HttpPatch("{id}/toggle-featured")]
        public async Task<IActionResult> ToggleFeatured(int id)
        {
            using var transaction = unitOfWork.BeginTransaction();
            try
            {
                if (!await ProductExists(id))
                {
                    return NotFound(new { message = $"Product with ID {id} not found." });
                }

                var result = await productService.ToggleFeaturedStatusAsync(id);

                if (!result)
                {
                    return StatusCode(500, new { message = "Failed to toggle featured status." });
                }

                await unitOfWork.CommitTransactionAsync();
                return Ok(new { message = "Product featured status toggled successfully." });
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackTransactionAsync();
                return StatusCode(500, new { message = "An error occurred while toggling featured status.", error = ex.Message });
            }
        }

        [HttpPatch("{id}/stock")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockDto updateStockDto)
        {
            using var transaction = unitOfWork.BeginTransaction();
            try
            {
                if (!await ProductExists(id))
                {
                    return NotFound(new { message = $"Product with ID {id} not found." });
                }

                var result = await productService.UpdateStockQuantityAsync(id, updateStockDto.Quantity);

                if (!result)
                {
                    return StatusCode(500, new { message = "Failed to update stock quantity." });
                }

                await unitOfWork.CommitTransactionAsync();
                return Ok(new { message = "Stock quantity updated successfully." });
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackTransactionAsync();
                return StatusCode(500, new { message = "An error occurred while updating stock quantity.", error = ex.Message });
            }
        }

        private async Task<bool> ProductExists(int id)
        {
            return await productService.ProductExistsAsync(id);
        }
    }
}