using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using eShop.Core.Models;
using eShop.Core.Services.Abstractions;
using eShop.Core.DTOs;
using AutoMapper;

namespace eShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;
        private readonly IMapper _mapper;

        public CategoriesController(IUnitOfWork unitOfWork, IFileService fileService, IMapper mapper)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories(
            [FromQuery] bool includeSubCategories = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var includes = includeSubCategories
                    ? new[] { nameof(Category.ChildCategories), nameof(Category.ParentCategory), nameof(Category.Products) }
                    : new[] { nameof(Category.ParentCategory), nameof(Category.Products) };

                var categories = await _unitOfWork.CategoryRepository.GetAllPagedAsync((page - 1) * pageSize, pageSize, includes);

                if (categories == null)
                {
                    return Ok(new List<CategoryDto>());
                }

                var categoryDtos = _mapper.Map<IEnumerable<CategoryDto>>(categories);
                return Ok(categoryDtos);
            }
            catch (Exception ex)
            {
                // Log the exception
                // _logger.LogError(ex, "Error occurred while getting categories");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id, [FromQuery] bool includeSubCategories = false)
        {
            var includes = includeSubCategories
                ? new[] { nameof(Category.ChildCategories), nameof(Category.ParentCategory), nameof(Category.Products) }
                : new[] { nameof(Category.ParentCategory), nameof(Category.Products) };

            var category = await _unitOfWork.CategoryRepository.GetByIdAsync(id, includes);

            if (category == null)
            {
                return NotFound();
            }

            var categoryDto = _mapper.Map<CategoryDto>(category);
            return Ok(categoryDto);
        }

        // GET: api/Categories/summary
        [HttpGet("summary")]
        public async Task<ActionResult<IEnumerable<CategorySummaryDto>>> GetCategoriesSummary()
        {
            var categories = await _unitOfWork.CategoryRepository.GetAllAsync(
                new[] { nameof(Category.ParentCategory), nameof(Category.Products) });
            var categorySummaryDtos = _mapper.Map<IEnumerable<CategorySummaryDto>>(categories);
            return Ok(categorySummaryDtos);
        }

        // GET: api/Categories/tree
        [HttpGet("tree")]
        public async Task<ActionResult<IEnumerable<CategoryTreeDto>>> GetCategoriesTree()
        {
            var rootCategories = await _unitOfWork.CategoryRepository.FindAllAsync(
                c => c.ParentCategoryId == null ,
                new[] { nameof(Category.ChildCategories) });

            var categoryTreeDtos = _mapper.Map<IEnumerable<CategoryTreeDto>>(rootCategories);
            return Ok(categoryTreeDtos);
        }

        // GET: api/Categories/active
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetActiveCategories(
            [FromQuery] bool includeSubCategories = false)
        {
            var includes = includeSubCategories
                ? new[] { nameof(Category.ChildCategories), nameof(Category.ParentCategory), nameof(Category.Products) }
                : new[] { nameof(Category.ParentCategory), nameof(Category.Products) };

            var categories = await _unitOfWork.CategoryRepository.FindAllAsync(c => c.IsActive, includes);
            var categoryDtos = _mapper.Map<IEnumerable<CategoryDto>>(categories);
            return Ok(categoryDtos);
        }

        // POST: api/Categories
        [HttpPost]
        [Authorize(Policy = "RequireAdminRole")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<CategoryDto>> CreateCategory([FromForm] CreateCategoryDto categoryDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate ParentCategoryId
            if (categoryDto.ParentCategoryId.HasValue)
            {
                var parentCategory = await _unitOfWork.CategoryRepository.GetByIdAsync(categoryDto.ParentCategoryId.Value);
                if (parentCategory == null)
                {
                    return BadRequest("Invalid ParentCategoryId");
                }
            }

            var category = _mapper.Map<Category>(categoryDto);

            // Handle image file uploads
            if (categoryDto.ImageFiles != null && categoryDto.ImageFiles.Any())
            {
                try
                {
                    var imageUrls = await _fileService.SaveFilesAsync(categoryDto.ImageFiles, "categories");
                    category.ImageUrls.AddRange(imageUrls);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            await _unitOfWork.CategoryRepository.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            // Reload with includes for proper DTO mapping
            var createdCategory = await _unitOfWork.CategoryRepository.GetByIdAsync(
                category.Id,
                new[] { nameof(Category.ParentCategory), nameof(Category.Products) });

            var createdCategoryDto = _mapper.Map<CategoryDto>(createdCategory);
            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, createdCategoryDto);
        }

        // PUT: api/Categories/5
        [HttpPut("{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateCategory(int id, [FromForm] UpdateCategoryDto updateCategoryDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingCategory = await _unitOfWork.CategoryRepository.GetByIdAsync(id,
                new[] { nameof(Category.ChildCategories), nameof(Category.ParentCategory) });

            // Validate Category Existing
            if (existingCategory == null)
            {
                return NotFound();
            }

            // Validate ParentCategoryId
            if (updateCategoryDto.ParentCategoryId.HasValue)
            {
                if (updateCategoryDto.ParentCategoryId == id || await HasCircularReference(id, updateCategoryDto.ParentCategoryId.Value))
                {
                    return BadRequest("Invalid ParentCategoryId: Circular reference detected");
                }

                var parentCategory = await _unitOfWork.CategoryRepository.GetByIdAsync(updateCategoryDto.ParentCategoryId.Value);
                if (parentCategory == null)
                {
                    return BadRequest("Invalid ParentCategoryId");
                }
            }

            // Map the DTO to the existing entity
            _mapper.Map(updateCategoryDto, existingCategory);

            // Handle image removal
            if (updateCategoryDto.ImageUrlsToRemove != null && updateCategoryDto.ImageUrlsToRemove.Any())
            {
                foreach (var imageUrlToRemove in updateCategoryDto.ImageUrlsToRemove)
                {
                    existingCategory.ImageUrls.Remove(imageUrlToRemove);
                    await _fileService.DeleteFileAsync(imageUrlToRemove);
                }
            }

            // Handle new image uploads
            if (updateCategoryDto.ImageFiles != null && updateCategoryDto.ImageFiles.Any())
            {
                try
                {
                    var newImageUrls = await _fileService.SaveFilesAsync(updateCategoryDto.ImageFiles, "categories");
                    existingCategory.ImageUrls.AddRange(newImageUrls);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            await _unitOfWork.CategoryRepository.UpdateAsync(existingCategory);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Categories/5
        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _unitOfWork.CategoryRepository.GetByIdAsync(id,
                new[] { nameof(Category.ChildCategories), nameof(Category.Products) });

            if (category == null)
            {
                return NotFound();
            }

            if (category.ChildCategories.Any())
            {
                return BadRequest("Cannot delete category with child categories");
            }

            if (category.Products.Any())
            {
                return BadRequest("Cannot delete category with associated products");
            }

            // Delete associated images
            foreach (var imageUrl in category.ImageUrls)
            {
                await _fileService.DeleteFileAsync(imageUrl);
            }

            var parentCategoryId = category.ParentCategoryId;

            await _unitOfWork.CategoryRepository.RemoveAsync(category);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { id = category.Id, parentCategoryId }); // ✅ return this
        }


        // PUT: api/Categories/5/toggle-status
        [HttpPut("{id}/toggle-status")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<IActionResult> ToggleCategoryStatus(int id)
        {
            var category = await _unitOfWork.CategoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            category.IsActive = !category.IsActive;
            await _unitOfWork.CategoryRepository.UpdateAsync(category);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Categories/5/children
        [HttpGet("{id}/children")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategoryChildren(int id)
        {
            var parentCategory = await _unitOfWork.CategoryRepository.GetByIdAsync(id);
            if (parentCategory == null)
            {
                return NotFound();
            }

            var childCategories = await _unitOfWork.CategoryRepository.FindAllAsync(
                c => c.ParentCategoryId == id,
                new[] { nameof(Category.ParentCategory), nameof(Category.Products) });

            var categoryDtos = _mapper.Map<IEnumerable<CategoryDto>>(childCategories);
            return Ok(categoryDtos);
        }

        private async Task<bool> HasCircularReference(int categoryId, int parentCategoryId)
        {
            var currentId = parentCategoryId;
            var visitedIds = new HashSet<int> { categoryId }; // Prevent infinite loops

            while (currentId != 0)
            {
                if (visitedIds.Contains(currentId))
                {
                    return true; // Circular reference detected
                }

                visitedIds.Add(currentId);

                var currentCategory = await _unitOfWork.CategoryRepository.GetByIdAsync(currentId);
                if (currentCategory == null)
                {
                    return false;
                }

                if (currentCategory.ParentCategoryId == categoryId)
                {
                    return true;
                }

                currentId = currentCategory.ParentCategoryId ?? 0;
            }

            return false;
        }
    }
}