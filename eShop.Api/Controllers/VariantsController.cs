using Microsoft.AspNetCore.Mvc;
using eShop.Core.DTOs;
using eShop.Core.Services.Abstractions;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace eShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VariantsController : ControllerBase
    {
        private readonly IVariantService _variantService;

        public VariantsController(IVariantService variantService)
        {
            _variantService = variantService;
        }

        // GET: api/Variants/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<VariantDTO>> GetVariant(int id)
        {
            var variant = await _variantService.GetVariantByIdAsync(id);
            if (variant == null)
            {
                return NotFound();
            }
            return Ok(variant);
        }

        // GET: api/Variants/product/{productId}
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<IEnumerable<VariantDTO>>> GetVariantsByProductId(int productId)
        {
            var variants = await _variantService.GetVariantsByProductIdAsync(productId);
            return Ok(variants);
        }

        // POST: api/Variants
        [HttpPost]
        public async Task<ActionResult<VariantDTO>> CreateVariant(CreateVariantDTO variantDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdVariant = await _variantService.CreateVariantAsync(variantDto);
            return CreatedAtAction(nameof(GetVariant), new { id = createdVariant.Id }, createdVariant);
        }

        // PUT: api/Variants
        [HttpPut]
        public async Task<ActionResult<VariantDTO>> UpdateVariant(UpdateVariantDTO variantDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedVariant = await _variantService.UpdateVariantAsync(variantDto);
            if (updatedVariant == null)
            {
                return NotFound();
            }
            return Ok(updatedVariant);
        }

        // DELETE: api/Variants/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteVariant(int id)
        {
            var result = await _variantService.DeleteVariantAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }

        // PATCH: api/Variants/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<ActionResult> ToggleVariantStatus(int id)
        {
            var result = await _variantService.ToggleVariantStatusAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }

        // PATCH: api/Variants/{id}/stock
        [HttpPatch("{id}/stock")]
        public async Task<ActionResult> UpdateStockQuantity(int id, [FromBody] int quantity)
        {
            if (quantity < 0)
            {
                return BadRequest("Quantity cannot be negative");
            }

            var result = await _variantService.UpdateStockQuantityAsync(id, quantity);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}