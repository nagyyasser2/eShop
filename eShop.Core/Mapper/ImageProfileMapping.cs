using AutoMapper;
using eShop.Core.Models;
using eShop.Core.DTOs;

namespace eShop.Core.Mapper
{
    public class ImageProfileMapping : Profile
    {
        public ImageProfileMapping()
        {
            // CreateImageDTO -> Image
            CreateMap<CreateImageDTO, Image>();

           

            // UpdateImageDTO -> Image (only map non-null values)
            CreateMap<UpdateImageDTO, Image>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
