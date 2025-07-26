using AutoMapper;
using eShop.Core.DTOs;
using eShop.Core.Services.Abstractions;
using eShop.Core.Models;

namespace eShop.EF.Services
{
    public class ImageService : IImageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ImageService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ImageDTO?> GetImageByIdAsync(int id)
        {
            var image = await _unitOfWork.ImageRepository.GetByIdAsync(id, new[] { "Product" });
            return _mapper.Map<ImageDTO>(image);
        }

        public async Task<IEnumerable<ImageDTO>> GetAllImagesAsync()
        {
            var images = await _unitOfWork.ImageRepository.GetAllAsync(new[] { "Product" });
            return _mapper.Map<IEnumerable<ImageDTO>>(images);
        }

        public async Task<IEnumerable<ImageDTO>> GetImagesByProductIdAsync(int productId)
        {
            var images = await _unitOfWork.ImageRepository.FindAllAsync(i => i.ProductId == productId, new[] { "Product" });
            return _mapper.Map<IEnumerable<ImageDTO>>(images);
        }

        public async Task<ImageDTO> CreateImageAsync(CreateImageDTO imageDto)
        {
            var image = _mapper.Map<Image>(imageDto);

            // If setting as primary, ensure no other images are primary
            if (image.IsPrimary)
            {
                await ResetPrimaryImagesForProduct(image.ProductId);
            }

            var createdImage = await _unitOfWork.ImageRepository.AddAsync(image);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<ImageDTO>(createdImage);
        }

        public async Task<ImageDTO?> UpdateImageAsync(UpdateImageDTO imageDto)
        {
            var existingImage = await _unitOfWork.ImageRepository.GetByIdAsync(imageDto.Id);
            if (existingImage == null) return null;

            _mapper.Map(imageDto, existingImage);

            // If setting as primary, ensure no other images are primary
            if (imageDto.IsPrimary.HasValue && imageDto.IsPrimary.Value)
            {
                await ResetPrimaryImagesForProduct(existingImage.ProductId);
                existingImage.IsPrimary = true;
            }

            var updatedImage = _unitOfWork.ImageRepository.Update(existingImage);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<ImageDTO>(updatedImage);
        }

        public async Task<bool> DeleteImageAsync(int id)
        {
            var image = await _unitOfWork.ImageRepository.GetByIdAsync(id);
            if (image == null) return false;

            _unitOfWork.ImageRepository.Remove(image);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetAsPrimaryImageAsync(int id)
        {
            var image = await _unitOfWork.ImageRepository.GetByIdAsync(id);
            if (image == null) return false;

            await ResetPrimaryImagesForProduct(image.ProductId);

            image.IsPrimary = true;
            _unitOfWork.ImageRepository.Update(image);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateSortOrderAsync(int id, int sortOrder)
        {
            var image = await _unitOfWork.ImageRepository.GetByIdAsync(id);
            if (image == null) return false;

            image.SortOrder = sortOrder;
            _unitOfWork.ImageRepository.Update(image);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private async Task ResetPrimaryImagesForProduct(int productId)
        {
            var primaryImages = await _unitOfWork.ImageRepository.FindAllAsync(i =>
                i.ProductId == productId && i.IsPrimary);

            foreach (var img in primaryImages)
            {
                img.IsPrimary = false;
                _unitOfWork.ImageRepository.Update(img);
            }
        }
    }
}
