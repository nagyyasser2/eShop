using eShop.Core.DTOs.Products;
using eShop.Core.Models;
using AutoMapper;

namespace eShop.Core.MappingProfiles
{
    public class ProductProfile: Profile
    {
        public ProductProfile()
        {
            CreateMap<Product, CreateProductRequest>().ReverseMap();
            CreateMap<CreateProductImageDto, ProductImage>();
        }
    }
}
