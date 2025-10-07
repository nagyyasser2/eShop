using AutoMapper;
using eShop.Core.DTOs.Products;
using eShop.Core.DTOs.Category;
using eShop.Core.DTOs.Variants;
using eShop.Core.Models;

namespace eShop.Core.Mapper
{
    public class ProductMappingProfile : Profile
    {
        public ProductMappingProfile()
        {
            // CreateProductDto -> Product
            CreateMap<CreateProductRequest, Product>()
                .ForMember(dest => dest.Variants, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

          
            // UpdateProductDto -> Product
            CreateMap<UpdateProductRequest, Product>()
                .ForMember(dest => dest.Variants, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());



            // Variant -> VariantDTO
            CreateMap<Variant, VariantDto>();

            // Product -> ProductDTO
            CreateMap<Product, ProductDto>();

            // Category -> CategoryDto
            CreateMap<Category, CategoryDto>();

        }
    }
}