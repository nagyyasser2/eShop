using AutoMapper;
using eShop.Core.DTOs;
using eShop.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.Mapper
{
    public class CategoryMappingProfile : Profile
    {
        public CategoryMappingProfile()
        {
            CreateMap<Category, CategoryTreeDto>()
                .ForMember(dest => dest.Children, opt => opt.MapFrom(src => src.ChildCategories));
        }
    }
}
