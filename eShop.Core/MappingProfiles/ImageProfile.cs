using eShop.Core.DTOs.Images;
using eShop.Core.Models;
using AutoMapper;

namespace eShop.Core.MappingProfiles
{
    public class ImageProfile: Profile
    {
        public ImageProfile() { 
            CreateMap<CreateImageRequest ,Image>();
            CreateMap<Image, ImageDto>();
        }
    }
}
