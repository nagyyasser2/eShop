using eShop.Core.Services.Abstractions;
using eShop.Core.DTOs.Products;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using eShop.Core.Models;

namespace eShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController(IProductService productService): ControllerBase
    {
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CreateProductRequest createProductDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return Ok(await productService.CreateProductAsync(createProductDto));
        }

        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateProductRequest updateProductRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await productService.UpdateProductAsync(id, updateProductRequest);
            return Ok(new { message = "Product deleted successfully." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await productService.DeleteProductAsync(id);
            return Ok(new { message = "Product deleted successfully." });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOneById(int id)
        {
            var product = await productService.GetProductByIdAsync(id);

            if (product == null)
            {
                return NotFound(new { message = $"Product with ID {id} not found." });
            }

            return Ok(product);
        }

        [HttpGet]
        public async Task<IActionResult> Get(
        [FromQuery] string[]? tags,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? featured = null,
        [FromQuery] bool? active = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] int? daysBack = null) 
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;

                int skip = (page - 1) * pageSize;
                Expression<Func<Product, bool>> filter = p => true;

                var (products, totalCount) = await productService.GetFilteredPagedAsync(filter, skip, pageSize);
                var result = new { data = products, count = totalCount, page, pageSize };
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving products.", error = ex.Message });
            }
        }
    }
}