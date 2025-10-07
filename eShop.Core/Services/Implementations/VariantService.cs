using AutoMapper;
using eShop.Core.DTOs.Variants;
using eShop.Core.Models;
using eShop.Core.Services.Abstractions;

namespace eShop.Core.Services.Implementations
{
    public class VariantService : IVariantService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public VariantService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<VariantDto?> GetVariantByIdAsync(int id)
        {
            var variant = await _unitOfWork.VariantRepository.GetByIdAsync(id, new[] { "Product" });
            return _mapper.Map<VariantDto>(variant);
        }

        public async Task<IEnumerable<VariantDto>> GetVariantsByProductIdAsync(int productId)
        {
            var variants = await _unitOfWork.VariantRepository.FindAllAsync(v => v.ProductId == productId, new[] { "Product" });
            return _mapper.Map<IEnumerable<VariantDto>>(variants);
        }

        public async Task<VariantDto> CreateVariantAsync(CreateVariantRequest variantDto)
        {
            var variant = _mapper.Map<Variant>(variantDto);
            var createdVariant = await _unitOfWork.VariantRepository.AddAsync(variant);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<VariantDto>(createdVariant);
        }

        public async Task<VariantDto?> UpdateVariantAsync(UpdateVariantRequest variantDto)
        {
            var existingVariant = await _unitOfWork.VariantRepository.GetByIdAsync(variantDto.Id);
            if (existingVariant == null) return null;

            _mapper.Map(variantDto, existingVariant);
            var updatedVariant = _unitOfWork.VariantRepository.Update(existingVariant);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<VariantDto>(updatedVariant);
        }

        public async Task<bool> DeleteVariantAsync(int id)
        {
            var variant = await _unitOfWork.VariantRepository.GetByIdAsync(id);
            if (variant == null) return false;

            _unitOfWork.VariantRepository.Remove(variant);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleVariantStatusAsync(int id)
        {
            var variant = await _unitOfWork.VariantRepository.GetByIdAsync(id);
            if (variant == null) return false;

            variant.IsActive = !variant.IsActive;
            _unitOfWork.VariantRepository.Update(variant);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStockQuantityAsync(int id, int quantity)
        {
            var variant = await _unitOfWork.VariantRepository.GetByIdAsync(id);
            if (variant == null) return false;

            variant.StockQuantity = quantity;
            _unitOfWork.VariantRepository.Update(variant);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
    }
