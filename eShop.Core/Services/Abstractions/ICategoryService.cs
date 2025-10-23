using eShop.Core.DTOs.Category;

namespace eShop.Core.Services.Abstractions
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetPagedAsync(int page, int pageSize, bool includeSubCategories);
        Task<CategoryDto?> GetByIdAsync(int id, bool includeSubCategories);
        Task<IEnumerable<CategoryDto>> GetActiveAsync(bool includeSubCategories);
        Task<CategoryDto> CreateAsync(CreateCategoryDto categoryDto);
        Task<bool> UpdateAsync(int id, UpdateCategoryDto updateCategoryDto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ToggleStatusAsync(int id);
    }
}
