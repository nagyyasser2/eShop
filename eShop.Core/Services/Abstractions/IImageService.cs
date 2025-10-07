using eShop.Core.DTOs.Images;

namespace eShop.Core.Services.Abstractions
{
    public interface IImageService
    {
        Task<ImageDto?> GetImageByIdAsync(int id);
        Task<ImageDto> CreateImageAsync(CreateImageRequest imageDto);
        Task<bool> DeleteImageAsync(int id);
        Task<ImageDto?> SetAsPrimaryImageAsync(int id);
    }
}
