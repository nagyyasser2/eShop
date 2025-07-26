using eShop.Core.DTOs;

namespace eShop.Core.Services.Abstractions
{
    public interface IImageService
    {
        Task<ImageDTO?> GetImageByIdAsync(int id);
        Task<IEnumerable<ImageDTO>> GetAllImagesAsync();
        Task<IEnumerable<ImageDTO>> GetImagesByProductIdAsync(int productId);
        Task<ImageDTO> CreateImageAsync(CreateImageDTO imageDto);
        Task<ImageDTO?> UpdateImageAsync(UpdateImageDTO imageDto);
        Task<bool> DeleteImageAsync(int id);
        Task<bool> SetAsPrimaryImageAsync(int id);
        Task<bool> UpdateSortOrderAsync(int id, int sortOrder);
    }
}
