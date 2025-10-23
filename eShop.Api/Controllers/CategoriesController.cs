using eShop.Core.DTOs.Category;
using eShop.Core.Models;
using eShop.Core.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController(ICategoryService categoryService) : ControllerBase
    {
        private readonly ICategoryService _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));

        //[HttpPost] 
        //[Authorize(Policy = "RequireAdminRole")] 
        //[Consumes("multipart/form-data")] 
        //public async Task<ActionResult<CategoryDto>> CreateCategory([FromForm] CreateCategoryDto categoryDto) { 
        //    if (!ModelState.IsValid) 
        //    { 
        //        return BadRequest(ModelState);
        //    } 
        //    if (categoryDto.ParentCategoryId.HasValue) 
        //    { 
        //        var parentCategory = await _unitOfWork.CategoryRepository.GetByIdAsync(categoryDto.ParentCategoryId.Value);
        //        if (parentCategory == null) 
        //        { 
        //            return BadRequest("Invalid ParentCategoryId");
        //        }
        //    } 

        //    var category = _mapper.Map<Category>(categoryDto);

        //    (categoryDto.ImageFiles != null && categoryDto.ImageFiles.Any()) {
        //        try { 
        //            var imageUrls = await _fileService.SaveFilesAsync(categoryDto.ImageFiles, "categories");
        //            category.ImageUrls.AddRange(imageUrls);
        //        } catch (ArgumentException ex) { 
        //            return BadRequest(ex.Message); 
        //        } 
        //    } 
        //    await _unitOfWork.CategoryRepository.AddAsync(category);
        //    await _unitOfWork.SaveChangesAsync(); 
        //    var createdCategory = await _unitOfWork.CategoryRepository.GetByIdAsync( category.Id, new[] { nameof(Category.ParentCategory), nameof(Category.Products) });
        //    var createdCategoryDto = _mapper.Map<CategoryDto>(createdCategory);
        //    return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, createdCategoryDto);
        //}
        
        //[HttpPut("{id}")] 
        //[Authorize(Policy = "RequireAdminRole")] 
        //[Consumes("multipart/form-data")] 
        //public async Task<IActionResult> UpdateCategory(int id, [FromForm] UpdateCategoryDto updateCategoryDto)
        //{ 
        //    if (!ModelState.IsValid) 
        //    { 
        //        return BadRequest(ModelState);
        //    }
        //    var existingCategory = await _unitOfWork.CategoryRepository.GetByIdAsync(id, new[] { nameof(Category.ChildCategories), nameof(Category.ParentCategory) });
        //    if (existingCategory == null) 
        //    { 
        //        return NotFound();
        //    } 
        //    if (updateCategoryDto.ParentCategoryId.HasValue)
        //    { 
        //        if (updateCategoryDto.ParentCategoryId == id || await HasCircularReference(id, updateCategoryDto.ParentCategoryId.Value))
        //        { 
        //            return BadRequest("Invalid ParentCategoryId: Circular reference detected");
        //        } 
        //        var parentCategory = await _unitOfWork.CategoryRepository.GetByIdAsync(updateCategoryDto.ParentCategoryId.Value);
        //        if (parentCategory == null) 
        //        { 
        //            return BadRequest("Invalid ParentCategoryId"); 
        //        } 
        //    } 
        //      _mapper.Map(updateCategoryDto, existingCategory); 
        //    if (updateCategoryDto.ImageUrlsToRemove != null && updateCategoryDto.ImageUrlsToRemove.Any()) 
        //    { foreach (var imageUrlToRemove in updateCategoryDto.ImageUrlsToRemove) { existingCategory.ImageUrls.Remove(imageUrlToRemove); await _fileService.DeleteFileAsync(imageUrlToRemove); } } // Handle new image uploads if (updateCategoryDto.ImageFiles != null && updateCategoryDto.ImageFiles.Any()) { try { var newImageUrls = await _fileService.SaveFilesAsync(updateCategoryDto.ImageFiles, "categories"); existingCategory.ImageUrls.AddRange(newImageUrls); } catch (ArgumentException ex) { return BadRequest(ex.Message); } } await _unitOfWork.CategoryRepository.UpdateAsync(existingCategory); await _unitOfWork.SaveChangesAsync(); return NoContent(); } [HttpDelete("{id}")] [Authorize(Policy = "RequireAdminRole")] public async Task<IActionResult> DeleteCategory(int id) { var category = await _unitOfWork.CategoryRepository.GetByIdAsync(id, new[] { nameof(Category.ChildCategories), nameof(Category.Products) }); if (category == null) { return NotFound(); } if (category.ChildCategories.Any()) { return BadRequest("Cannot delete category with child categories"); } if (category.Products.Any()) { return BadRequest("Cannot delete category with associated products"); } // Delete associated images foreach (var imageUrl in category.ImageUrls) { await _fileService.DeleteFileAsync(imageUrl); } var parentCategoryId = category.ParentCategoryId; await _unitOfWork.CategoryRepository.RemoveAsync(category); await _unitOfWork.SaveChangesAsync(); return Ok(new { id = category.Id, parentCategoryId }); }

        [HttpGet]
        public async Task<IActionResult> GetCategories(
            [FromQuery] bool includeSubCategories = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var categories = await _categoryService.GetPagedAsync(page, pageSize, includeSubCategories);
            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(int id, [FromQuery] bool includeSubCategories = false)
        {
            var category = await _categoryService.GetByIdAsync(id, includeSubCategories);
            return category is null ? NotFound() : Ok(category);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive([FromQuery] bool includeSubCategories = false) =>
            Ok(await _categoryService.GetActiveAsync(includeSubCategories));

        [HttpPost]
        [Authorize(Policy = "RequireAdminRole")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CreateCategoryDto dto)
        {
            var result = await categoryService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetCategory), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateCategoryDto dto)
        {
            var success = await categoryService.UpdateAsync(id, dto);
            return success ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await categoryService.DeleteAsync(id);
            return success ? Ok(new { id }) : BadRequest("Cannot delete category with children or products.");
        }

        [HttpPut("{id}/toggle-status")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var success = await categoryService.ToggleStatusAsync(id);
            return success ? NoContent() : NotFound();
        }
    }
}
