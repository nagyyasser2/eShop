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
