using eShop.Core.DTOs.Products;
using eShop.Core.Models;
using AutoMapper;

namespace eShop.Core.MappingProfiles
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            // Create → Entity
            CreateMap<CreateProductRequest, Product>().ReverseMap();

            // Update → Entity (exclude ProductImages since we handle it manually)
            CreateMap<UpdateProductRequest, Product>()
                .ForMember(dest => dest.ProductImages, opt => opt.Ignore())
                .ReverseMap();

            // Image mappings
            CreateMap<CreateProductImageRequest, ProductImage>();
            CreateMap<UpdateProductImageRequest, ProductImage>(); // Add this line
            CreateMap<CreateProductImageDto, ProductImage>(); // Add if needed

            // Entity → DTO
            CreateMap<Product, ProductDto>();

            // Map for images
            CreateMap<ProductImage, ProductImageDto>();
        }
    }
}