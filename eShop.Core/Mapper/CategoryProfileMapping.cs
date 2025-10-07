using AutoMapper;
using eShop.Core.DTOs.Category;
using eShop.Core.Models;

namespace eShop.Core.Mapper
{
    public class CategoryMappingProfile : Profile
    {
        public CategoryMappingProfile()
        {
            // Category -> CategoryDto
            CreateMap<Category, CategoryDto>()
                .ForMember(dest => dest.ParentCategoryName, opt => opt.MapFrom(src => src.ParentCategory != null ? src.ParentCategory.Name : null))
                .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.Products != null ? src.Products.Count : 0))
                .ForMember(dest => dest.ChildCategories, opt => opt.MapFrom(src => src.ChildCategories));

            // CategoryDto -> Category
            CreateMap<CategoryDto, Category>();

            // CreateCategoryDto -> Category
            CreateMap<CreateCategoryDto, Category>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.ImageUrls, opt => opt.MapFrom(src => src.ImageUrls ?? new List<string>()))
                .ForMember(dest => dest.ChildCategories, opt => opt.Ignore()) // Prevent recursive loop
                .ForMember(dest => dest.Products, opt => opt.Ignore());

            // UpdateCategoryDto -> Category
            CreateMap<UpdateCategoryDto, Category>()
                .ForMember(dest => dest.ImageUrls, opt => opt.Ignore())   // Will be handled manually when merging images
                .ForMember(dest => dest.ChildCategories, opt => opt.Ignore())
                .ForMember(dest => dest.Products, opt => opt.Ignore());

            // Category -> CategorySummaryDto
            CreateMap<Category, CategorySummaryDto>()
                .ForMember(dest => dest.ParentCategoryName, opt => opt.MapFrom(src => src.ParentCategory != null ? src.ParentCategory.Name : null))
                .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.Products != null ? src.Products.Count : 0));

            // Category -> CategoryTreeDto (tree structure)
            CreateMap<Category, CategoryTreeDto>()
                .ForMember(dest => dest.Children, opt => opt.MapFrom(src => src.ChildCategories));
        }
    }
}
