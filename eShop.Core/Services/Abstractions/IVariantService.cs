using eShop.Core.DTOs.Variants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.Services.Abstractions
{
    public interface IVariantService
    {
        Task<VariantDto?> GetVariantByIdAsync(int id);
        Task<IEnumerable<VariantDto>> GetVariantsByProductIdAsync(int productId);
        Task<VariantDto> CreateVariantAsync(CreateVariantRequest variantDto);
        Task<VariantDto?> UpdateVariantAsync(UpdateVariantRequest variantDto);
        Task<bool> DeleteVariantAsync(int id);
        Task<bool> ToggleVariantStatusAsync(int id);
        Task<bool> UpdateStockQuantityAsync(int id, int quantity);
    }
}
