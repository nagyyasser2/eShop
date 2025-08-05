using System.Linq.Expressions;
using AutoMapper;
using eShop.Core.DTOs;
using eShop.Core.Models;
using eShop.Core.Services.Abstractions;

namespace eShop.EF.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IFileService _fileService;

        public ProductService(IUnitOfWork unitOfWork, IMapper mapper, IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _fileService = fileService;
        }

        public async Task<ProductDTO?> GetProductByIdAsync(int id)
        {
            var product = await _unitOfWork.ProductRepository.GetByIdAsync(id, new[] { "Category", "Images", "Variants" });
            return _mapper.Map<ProductDTO>(product);
        }

        public async Task<(IEnumerable<ProductDTO> Products, int TotalCount)> GetFilteredPagedAsync(
            Expression<Func<Product, bool>> filter,
            int skip,
            int take,
            string[]? includes = null)
        {
            // Default includes if none provided
            includes ??= new[] { "Category", "Images", "Variants" };

            // Get filtered and paginated products
            var products = await _unitOfWork.ProductRepository.GetFilteredPagedAsync(filter, skip, take, includes);

            // Get total count for pagination
            var totalCount = await _unitOfWork.ProductRepository.CountAsync(filter);

            // Map to DTOs
            var productDtos = _mapper.Map<IEnumerable<ProductDTO>>(products);

            return (productDtos, totalCount);
        }

        public async Task<ProductDTO> CreateProductAsync(CreateProductDto productDto)
        {
            var product = _mapper.Map<Product>(productDto);
            var createdProduct = await _unitOfWork.ProductRepository.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<ProductDTO>(createdProduct);
        }

        public async Task<ProductDTO?> UpdateProductAsync(UpdateProductDto productDto)
        {
            var existingProduct = await _unitOfWork.ProductRepository.GetByIdAsync(productDto.Id);
            if (existingProduct == null) return null;

            _mapper.Map(productDto, existingProduct);
            var updatedProduct = _unitOfWork.ProductRepository.Update(existingProduct);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<ProductDTO>(updatedProduct);
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _unitOfWork.ProductRepository.GetByIdAsync(id, new[] { "Images" });
            if (product == null) return false;

            // Delete associated image files
            foreach (var image in product.Images)
            {
                await _fileService.DeleteFileAsync(image.Url);
            }

            _unitOfWork.ProductRepository.Remove(product);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleProductStatusAsync(int id)
        {
            var product = await _unitOfWork.ProductRepository.GetByIdAsync(id);
            if (product == null) return false;

            product.IsActive = !product.IsActive;
            _unitOfWork.ProductRepository.Update(product);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleFeaturedStatusAsync(int id)
        {
            var product = await _unitOfWork.ProductRepository.GetByIdAsync(id);
            if (product == null) return false;

            product.IsFeatured = !product.IsFeatured;
            _unitOfWork.ProductRepository.Update(product);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStockQuantityAsync(int id, int quantity)
        {
            var product = await _unitOfWork.ProductRepository.GetByIdAsync(id);
            if (product == null) return false;

            product.StockQuantity = quantity;
            _unitOfWork.ProductRepository.Update(product);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ProductExistsAsync(int id)
        {
            var product = await _unitOfWork.ProductRepository.GetByIdAsync(id);
            return product != null;
        }

        public async Task<bool> ProductExistsBySKUAsync(string sku, int? excludeProductId = null)
        {
            var products = await _unitOfWork.ProductRepository.FindAllAsync(p => p.SKU == sku);
            if (excludeProductId.HasValue)
            {
                products = products.Where(p => p.Id != excludeProductId.Value);
            }
            return products.Any();
        }
    }
}