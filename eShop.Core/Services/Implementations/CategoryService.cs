using eShop.Core.Services.Abstractions;
using eShop.Core.DTOs.Category;
using eShop.Core.Models;
using AutoMapper;

namespace eShop.Core.Services.Implementations
{
    public class CategoryService(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IFileService fileService,
        IMapper mapper,
        CacheInvalidationHelper cacheInvalidation) : ICategoryService
    {
        private const string CATEGORY_LIST_CACHE_PREFIX = "categories:list:";
        private const string CATEGORY_DETAIL_CACHE_PREFIX = "categories:detail:";

        public async Task<IEnumerable<CategoryDto>> GetPagedAsync(int page, int pageSize, bool includeSubCategories)
        {
            string cacheKey = $"{CATEGORY_LIST_CACHE_PREFIX}{page}-{pageSize}-{includeSubCategories}";
            var cached = await cacheService.GetAsync<IEnumerable<CategoryDto>>(cacheKey);
            if (cached != null && cached.Any()) return cached;

            var includes = includeSubCategories
                ? new[] { nameof(Category.ChildCategories), nameof(Category.ParentCategory), nameof(Category.Products) }
                : new[] { nameof(Category.ParentCategory), nameof(Category.Products) };

            var categories = await unitOfWork.CategoryRepository.GetAllPagedAsync((page - 1) * pageSize, pageSize, includes);
            var categoryDtos = mapper.Map<IEnumerable<CategoryDto>>(categories);

            await cacheService.SetAsync(cacheKey, categoryDtos, TimeSpan.FromMinutes(10));
            return categoryDtos;
        }

        public async Task<CategoryDto?> GetByIdAsync(int id, bool includeSubCategories)
        {
            string cacheKey = $"{CATEGORY_DETAIL_CACHE_PREFIX}{id}-{includeSubCategories}";
            var cached = await cacheService.GetAsync<CategoryDto>(cacheKey);
            if (cached != null) return cached;

            var includes = includeSubCategories
                ? new[] { nameof(Category.ChildCategories), nameof(Category.ParentCategory), nameof(Category.Products) }
                : new[] { nameof(Category.ParentCategory), nameof(Category.Products) };

            var category = await unitOfWork.CategoryRepository.GetByIdAsync(id, includes);
            if (category == null) return null;

            var dto = mapper.Map<CategoryDto>(category);
            await cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10));

            return dto;
        }

        public async Task<IEnumerable<CategoryDto>> GetActiveAsync(bool includeSubCategories)
        {
            string cacheKey = $"{CATEGORY_LIST_CACHE_PREFIX}active-{includeSubCategories}";
            var cached = await cacheService.GetAsync<IEnumerable<CategoryDto>>(cacheKey);
            if (cached != null && cached.Any()) return cached;

            var includes = includeSubCategories
                ? new[] { nameof(Category.ChildCategories), nameof(Category.ParentCategory), nameof(Category.Products) }
                : new[] { nameof(Category.ParentCategory), nameof(Category.Products) };

            var categories = await unitOfWork.CategoryRepository.FindAllAsync(c => c.IsActive, includes);
            var dtos = mapper.Map<IEnumerable<CategoryDto>>(categories);

            await cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(10));
            return dtos;
        }

        public async Task<CategoryDto> CreateAsync(CreateCategoryDto categoryDto)
        {
            var category = mapper.Map<Category>(categoryDto);

            if (categoryDto.ImageFiles != null && categoryDto.ImageFiles.Any())
            {
                var imageUrls = await fileService.SaveFilesAsync(categoryDto.ImageFiles, "categories");
                category.ImageUrls.AddRange(imageUrls);
            }

            await unitOfWork.CategoryRepository.AddAsync(category);
            await unitOfWork.SaveChangesAsync();

            await cacheInvalidation.InvalidateCategoryWithRelatedProductsAsync(category.Id);

            var created = await unitOfWork.CategoryRepository.GetByIdAsync(
                category.Id, new[] { nameof(Category.ParentCategory), nameof(Category.Products) });

            return mapper.Map<CategoryDto>(created);
        }

        public async Task<bool> UpdateAsync(int id, UpdateCategoryDto dto)
        {
            var category = await unitOfWork.CategoryRepository.GetByIdAsync(id,
                new[] { nameof(Category.ChildCategories), nameof(Category.ParentCategory) });

            if (category == null) return false;

            mapper.Map(dto, category);

            if (dto.ImageUrlsToRemove != null && dto.ImageUrlsToRemove.Any())
            {
                foreach (var url in dto.ImageUrlsToRemove)
                {
                    category.ImageUrls.Remove(url);
                    await fileService.DeleteFileAsync(url);
                }
            }

            if (dto.ImageFiles != null && dto.ImageFiles.Any())
            {
                var newUrls = await fileService.SaveFilesAsync(dto.ImageFiles, "categories");
                category.ImageUrls.AddRange(newUrls);
            }

            await unitOfWork.CategoryRepository.UpdateAsync(category);
            await unitOfWork.SaveChangesAsync();

            await cacheInvalidation.InvalidateCategoryWithRelatedProductsAsync(id);

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var category = await unitOfWork.CategoryRepository.GetByIdAsync(id,
                new[] { nameof(Category.ChildCategories), nameof(Category.Products) });

            if (category == null) return false;
            if (category.ChildCategories.Any() || category.Products.Any()) return false;

            foreach (var url in category.ImageUrls)
            {
                await fileService.DeleteFileAsync(url);
            }

            await unitOfWork.CategoryRepository.RemoveAsync(category);
            await unitOfWork.SaveChangesAsync();

            await cacheInvalidation.InvalidateCategoryWithRelatedProductsAsync(id);

            return true;
        }

        public async Task<bool> ToggleStatusAsync(int id)
        {
            var category = await unitOfWork.CategoryRepository.GetByIdAsync(id);
            if (category == null) return false;

            category.IsActive = !category.IsActive;
            await unitOfWork.CategoryRepository.UpdateAsync(category);
            await unitOfWork.SaveChangesAsync();

            await cacheInvalidation.InvalidateCategoryWithRelatedProductsAsync(id);

            return true;
        }
    }
}