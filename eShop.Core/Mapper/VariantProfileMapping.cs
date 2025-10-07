using AutoMapper;
using eShop.Core.Models;
using eShop.Core.DTOs.Variants;

namespace eShop.Core.Mapper
{
    public class VariantProfileMapping : Profile
    {
        public VariantProfileMapping()
        {
            // CreateVariantDTO -> Variant
            CreateMap<CreateVariantRequest, Variant>()
                .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId))
                .ForMember(dest => dest.Id, opt => opt.Ignore()); // Explicitly ignore Id (auto-generated)

            // Variant -> VariantDTO
            CreateMap<Variant, VariantDto>()
                .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId));

            // UpdateVariantDTO -> Variant
            CreateMap<UpdateVariantRequest, Variant>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id)) // Map Id explicitly
                .ForMember(dest => dest.ProductId, opt => opt.Ignore()) // Ignore ProductId (not in UpdateVariantDTO)
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // CreateVariantDTO -> UpdateVariantDTO
            CreateMap<CreateVariantRequest, UpdateVariantRequest>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // CreateVariantDTO doesn't contain Id
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // UpdateVariantDTO -> CreateVariantDTO
            CreateMap<UpdateVariantRequest, CreateVariantRequest>()
                .ForMember(dest => dest.ProductId, opt => opt.Ignore()) // ProductId might not be in UpdateVariantDTO
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}