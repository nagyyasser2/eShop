using AutoMapper;
using eShop.Core.DTOs;
using eShop.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.Mapper
{
    public class BannerProfile : Profile
    {
        public BannerProfile()
        {
            // Map from BannerCreateDto to Banner
            CreateMap<BannerCreateDto, Banner>()
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // Map from BannerUpdateDto to Banner
            CreateMap<BannerUpdateDto, Banner>()
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            // Map from Banner to BannerResponseDto
            CreateMap<Banner, BannerResponseDto>();
        }
    }
}
