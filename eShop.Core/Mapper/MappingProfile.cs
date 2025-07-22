using AutoMapper;
using eShop.Core.DTOs;
using eShop.Core.Models;

namespace eShop.Core.Mapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Category to CategoryDto mapping with circular reference protection
            CreateMap<Category, CategoryDto>()
                .ForMember(dest => dest.ParentCategoryName,
                          opt => opt.MapFrom(src => src.ParentCategory != null ? src.ParentCategory.Name : null))
                .ForMember(dest => dest.ProductCount,
                          opt => opt.MapFrom(src => src.Products != null ? src.Products.Count : 0))
                .ForMember(dest => dest.ChildCategories, opt => opt.Condition(src => src.ChildCategories != null))
                .MaxDepth(2); // Limit depth to prevent infinite recursion

            // CreateCategoryDto to Category mapping
            CreateMap<CreateCategoryDto, Category>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.ParentCategory, opt => opt.Ignore())
                .ForMember(dest => dest.ChildCategories, opt => opt.Ignore())
                .ForMember(dest => dest.Products, opt => opt.Ignore())
                .ForMember(dest => dest.ImageUrls, opt => opt.MapFrom(src => src.ImageUrls ?? new List<string>()));

            // UpdateCategoryDto to Category mapping
            CreateMap<UpdateCategoryDto, Category>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ParentCategory, opt => opt.Ignore())
                .ForMember(dest => dest.ChildCategories, opt => opt.Ignore())
                .ForMember(dest => dest.Products, opt => opt.Ignore());

            // Category to CategorySummaryDto mapping
            CreateMap<Category, CategorySummaryDto>()
                .ForMember(dest => dest.ParentCategoryName,
                          opt => opt.MapFrom(src => src.ParentCategory != null ? src.ParentCategory.Name : null))
                .ForMember(dest => dest.ProductCount,
                          opt => opt.MapFrom(src => src.Products != null ? src.Products.Count : 0));

            // Category to CategoryTreeDto mapping (for hierarchical display)
            CreateMap<Category, CategoryTreeDto>()
                .ForMember(dest => dest.Children, opt => opt.Condition(src => src.ChildCategories != null))
                .MaxDepth(3); // Limit depth for tree structure

            // Simple reverse mapping for updates
            CreateMap<CategoryDto, Category>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ParentCategory, opt => opt.Ignore())
                .ForMember(dest => dest.ChildCategories, opt => opt.Ignore())
                .ForMember(dest => dest.Products, opt => opt.Ignore());
        }
    }
}