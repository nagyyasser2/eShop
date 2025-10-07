using eShop.Core.Services.Abstractions;
using System.Linq.Expressions;
using eShop.Core.Models;
using eShop.Core.DTOs.Products;
using AutoMapper;

namespace eShop.Core.Services.Implementations
{
    public class ProductService(IUnitOfWork unitOfWork, IFileService fileService, IMapper mapper) : IProductService
    {
        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            // Define the navigation properties to eagerly load
            string[] includes = new string[] { "Images", "Category" };

            // Pass the includes array to GetByIdAsync
            var product = await unitOfWork.ProductRepository.GetByIdAsync(id, includes);

            // Check if the product was found before mapping
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
            includes ??= new[] { "Category", "Images", "Variants" };

            var products = await unitOfWork.ProductRepository.GetFilteredPagedAsync(filter, skip, take, includes);

            var totalCount = await unitOfWork.ProductRepository.CountAsync(filter);

            var productDTOs = mapper.Map<IEnumerable<ProductDto>>(products);

            return (productDTOs, totalCount);
        }

        public async Task<ProductDto?> CreateProductAsync(CreateProductRequest createProductDto)
        {
            using var transaction = unitOfWork.BeginTransaction();

            try
            {
                var product = mapper.Map<Product>(createProductDto);

                var createdProduct = await unitOfWork.ProductRepository.AddAsync(product);

                await unitOfWork.SaveChangesAsync();

                await unitOfWork.CommitTransactionAsync();

                return mapper.Map<ProductDto>(createdProduct);
            }
            catch (Exception)
            {
                await unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ProductDto?> UpdateProductAsync(UpdateProductRequest productDto)
        {
            var existingProduct = await unitOfWork.ProductRepository.GetByIdAsync(productDto.Id);
            if (existingProduct == null) return null;

            mapper.Map(productDto, existingProduct);

            var updatedProduct = unitOfWork.ProductRepository.Update(existingProduct);
            await unitOfWork.SaveChangesAsync();

            return mapper.Map<ProductDto>(updatedProduct);
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await unitOfWork.ProductRepository.GetByIdAsync(id, new[] { "Images" });

            if (product == null) return false;

            foreach (var productImage in product.Images)
            {
                await fileService.DeleteFileAsync(productImage.Path);
            }

            unitOfWork.ProductRepository.Remove(product);

            return await unitOfWork.SaveChangesAsync() > 0;
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
            var products = await unitOfWork.ProductRepository.FindAllAsync(p => p.SKU == sku);
            if (excludeProductId.HasValue)
            {
                products = products.Where(p => p.Id != excludeProductId.Value);
            }
            return products.Any();
        }
    }
}
