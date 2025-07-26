using AutoMapper;
using eShop.Core.DTOs;
using eShop.Core.Models;
using eShop.Core.Services.Abstractions;
using eShop.Core.Services.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public async Task<IEnumerable<CreateProductDto>> GetAllProductsAsync()
        {
            var products = await _unitOfWork.ProductRepository.GetAllAsync(new[] { "Category", "Images", "Variants" });
            return _mapper.Map<IEnumerable<CreateProductDto>>(products);
        }

        public async Task<IEnumerable<CreateProductDto>> GetActiveProductsAsync()
        {
            var products = await _unitOfWork.ProductRepository.FindAllAsync(p => p.IsActive, new[] { "Category", "Images", "Variants" });
            return _mapper.Map<IEnumerable<CreateProductDto>>(products);
        }

        public async Task<IEnumerable<CreateProductDto>> GetFeaturedProductsAsync()
        {
            var products = await _unitOfWork.ProductRepository.FindAllAsync(p => p.IsFeatured && p.IsActive, new[] { "Category", "Images", "Variants" });
            return _mapper.Map<IEnumerable<CreateProductDto>>(products);
        }

        public async Task<IEnumerable<CreateProductDto>> GetProductsByCategoryAsync(int categoryId)
        {
            var products = await _unitOfWork.ProductRepository.FindAllAsync(p => p.CategoryId == categoryId && p.IsActive, new[] { "Category", "Images", "Variants" });
            return _mapper.Map<IEnumerable<CreateProductDto>>(products);
        }

        public async Task<CreateProductDto> CreateProductAsync(CreateProductDto productDto)
        {
            var product = _mapper.Map<Product>(productDto);

            var createdProduct = await _unitOfWork.ProductRepository.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<CreateProductDto>(createdProduct);
        }

        public async Task<CreateProductDto?> UpdateProductAsync(CreateProductDto productDto)
        {
            var existingProduct = await _unitOfWork.ProductRepository.GetByIdAsync(productDto.Id);
            if (existingProduct == null) return null;

            _mapper.Map(productDto, existingProduct);

            var updatedProduct = _unitOfWork.ProductRepository.Update(existingProduct);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CreateProductDto>(updatedProduct);
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
