using AutoMapper;
using eShop.Core.DTOs;
using eShop.Core.Models;

namespace eShop.Core.Mapper
{
    public class ProductMappingProfile : Profile
    {
        public ProductMappingProfile()
        {
            // CreateProductDto -> Product
            CreateMap<CreateProductDto, Product>()
                .ForMember(dest => dest.Variants, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.Images, opt => opt.Ignore());

            // Product -> CreateProductDto
            CreateMap<Product, CreateProductDto>()
                .ForMember(dest => dest.Images, opt => opt.Ignore())
                .ForMember(dest => dest.Variants, opt => opt.Ignore());

            // UpdateProductDto -> Product
            CreateMap<UpdateProductDto, Product>()
                .ForMember(dest => dest.Variants, opt => opt.Ignore())
                .ForMember(dest => dest.Images, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            // Product -> UpdateProductDto
            CreateMap<Product, UpdateProductDto>()
                .ForMember(dest => dest.NewImages, opt => opt.Ignore())
                .ForMember(dest => dest.ImageIdsToDelete, opt => opt.Ignore())
                .ForMember(dest => dest.Variants, opt => opt.Ignore()); // Ignore Variants in reverse mapping

            // UpdateProductDto -> CreateProductDto (new mapping to fix the error)
            CreateMap<UpdateProductDto, CreateProductDto>()
                .ForMember(dest => dest.Images, opt => opt.Ignore())
                .ForMember(dest => dest.Variants, opt => opt.MapFrom(src => src.Variants));

            // CreateVariantDTO -> Variant
            CreateMap<CreateVariantDTO, Variant>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            // UpdateVariantDTO -> Variant
            CreateMap<UpdateVariantDTO, Variant>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ProductId, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Variant -> VariantDTO
            CreateMap<Variant, VariantDTO>();

            // Product -> ProductDTO
            CreateMap<Product, ProductDTO>();

            // Category -> CategoryDto
            CreateMap<Category, CategoryDto>();

            // Image -> ImageDTO
            CreateMap<Image, ImageDTO>();
        }
    }
}