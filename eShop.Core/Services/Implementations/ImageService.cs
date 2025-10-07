using eShop.Core.Services.Abstractions;
using eShop.Core.Models;
using eShop.Core.DTOs.Images;
using AutoMapper;

namespace eShop.Core.Services.Implementations
{
    public class ImageService(IUnitOfWork unitOfWork, IMapper mapper) : IImageService
    {
        public async Task<ImageDto> CreateImageAsync(CreateImageRequest imageDTO)
        {
            var image = mapper.Map<CreateImageRequest, Image>(imageDTO);

            image.IsAttached = true;

            var result = await unitOfWork.ImageRepository.AddAsync(image);

            await unitOfWork.SaveChangesAsync();

            return mapper.Map<Image, ImageDto>(result);
        }

        public async Task<bool> DeleteImageAsync(int id)
        {
            var image = await unitOfWork.ImageRepository.GetByIdAsync(id);

            if (image == null)
            {
                return false;
            }

            unitOfWork.ImageRepository.Remove(image);

            var changes = await unitOfWork.SaveChangesAsync();

            return changes > 0;
        }

        public async Task<ImageDto?> GetImageByIdAsync(int id)
        {
            var image = await unitOfWork.ImageRepository.GetByIdAsync(id);

            if (image == null)
            {
                return null;
            }

            return mapper.Map<Image, ImageDto>(image);
        }

        public async Task<ImageDto?> SetAsPrimaryImageAsync(int id)
        {
            var imageToSetPrimary = await unitOfWork.ImageRepository.GetByIdAsync(id);
            if (imageToSetPrimary == null)
            {
                return null;
            }

            if (!imageToSetPrimary.IsPrimary)
            {
                imageToSetPrimary.IsPrimary = true;
                unitOfWork.ImageRepository.Update(imageToSetPrimary);
            }

            await unitOfWork.SaveChangesAsync();

            return mapper.Map<Image, ImageDto>(imageToSetPrimary);
        }
    }
}