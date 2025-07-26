using AutoMapper;
using eShop.Core.DTOs;
using eShop.Core.Models;

namespace eShop.Core.Mapper
{
    public class ProductMappingProfile : Profile
    {
        public ProductMappingProfile()
        {
            CreateMap<CreateProductDto, Product>()
                .ForMember(dest => dest.Variants, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.Images, opt => opt.Ignore()); // Ignore Images as they are handled separately

            CreateMap<Product, CreateProductDto>()
                .ForMember(dest => dest.Images, opt => opt.Ignore()) // Ignore Images in reverse mapping
                .ForMember(dest => dest.Variants, opt => opt.Ignore()); // Ignore Variants in reverse mapping

            CreateMap<UpdateProductDto, Product>()
                .ForMember(dest => dest.Variants, opt => opt.Ignore()) // Ignore Variants as they are handled separately
                .ForMember(dest => dest.Images, opt => opt.Ignore()) // Ignore Images as they are handled separately
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()); // Prevent overwriting CreatedAt

            CreateMap<Product, UpdateProductDto>()
                .ForMember(dest => dest.NewImages, opt => opt.Ignore()) // Ignore NewImages in reverse mapping
                .ForMember(dest => dest.ImageIdsToDelete, opt => opt.Ignore()); // Ignore ImageIdsToDelete in reverse mapping

            CreateMap<CreateVariantDTO, Variant>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<UpdateVariantDTO, Variant>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()); // Prevent overwriting CreatedAt

            CreateMap<Variant, VariantDTO>();

            // New mappings for ProductDTO
            CreateMap<Product, ProductDTO>();
            CreateMap<Category, CategoryDto>();
            CreateMap<Image, ImageDTO>();
        }
    }
}