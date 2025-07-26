using AutoMapper;
using eShop.Core.Models;
using eShop.Core.DTOs;

namespace eShop.Core.Mapper
{
    public class VariantProfileMapping : Profile
    {
        public VariantProfileMapping()
        {
            // CreateVariantDTO -> Variant
            CreateMap<CreateVariantDTO, Variant>()
                .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId))
                .ForMember(dest => dest.Id, opt => opt.Ignore()); // Explicitly ignore Id (auto-generated)

            // Variant -> VariantDTO
            CreateMap<Variant, VariantDTO>()
                .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId));

            // UpdateVariantDTO -> Variant
            CreateMap<UpdateVariantDTO, Variant>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id)) // Map Id explicitly
                .ForMember(dest => dest.ProductId, opt => opt.Ignore()) // Ignore ProductId (not in UpdateVariantDTO)
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}