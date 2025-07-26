using eShop.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.Services.Abstractions
{
    public interface IVariantService
    {
        Task<VariantDTO?> GetVariantByIdAsync(int id);
        Task<IEnumerable<VariantDTO>> GetVariantsByProductIdAsync(int productId);
        Task<VariantDTO> CreateVariantAsync(CreateVariantDTO variantDto);
        Task<VariantDTO?> UpdateVariantAsync(UpdateVariantDTO variantDto);
        Task<bool> DeleteVariantAsync(int id);
        Task<bool> ToggleVariantStatusAsync(int id);
        Task<bool> UpdateStockQuantityAsync(int id, int quantity);
    }
}
