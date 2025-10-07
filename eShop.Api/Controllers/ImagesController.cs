using eShop.Core.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using eShop.Core.DTOs.Images;
using eShop.Core.Exceptions;
using eShop.Core.DTOs.Api;

namespace eShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController(IImageService imageService, IFileService fileService) : ControllerBase
    {
        private readonly IImageService _imageService = imageService;
        private readonly IFileService _fileService = fileService;

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<ImageDto>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetImage(int id)
        {
            var image = await _imageService.GetImageByIdAsync(id);

            if (image == null)
            {
                throw new NotFoundException($"Image with ID {id} not found.");
            }

            return Ok(new ApiResponse<ImageDto>
            {
                Success = true,
                Data = image,
                Message = $"Image {id} retrieved successfully."
            });
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ApiResponse<ImageDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse<ImageDto>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiResponse<ImageDto>
                {
                    Success = false,
                    Message = "Validation Error",
                    Errors = new List<string> { "No file provided or file is empty." }
                });
            }

            var subfolder = $"products";
            var savedFilePath = await _fileService.SaveFileAsync(file, subfolder);

            var createImageDto = new CreateImageRequest
            {
                Path = savedFilePath,
            };

            var createdImage = await _imageService.CreateImageAsync(createImageDto);

            var response = new ApiResponse<ImageDto>
            {
                Success = true,
                Data = createdImage,
                Message = "Image uploaded and created successfully."
            };

            return CreatedAtAction(nameof(GetImage), new { id = createdImage.Id }, response);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var imageToDelete = await _imageService.GetImageByIdAsync(id);

            if (imageToDelete == null)
            {
                throw new NotFoundException($"Image with ID {id} not found.");
            }

            await _imageService.DeleteImageAsync(id);

            return NoContent();
        }

        [HttpPatch("{id}/primary")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<ImageDto>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetPrimary(int id)
        {
            var updatedImage = await _imageService.SetAsPrimaryImageAsync(id);

            if (updatedImage == null)
            {
                throw new NotFoundException($"Image with ID {id} not found.");
            }

            return Ok(new ApiResponse<ImageDto>
            {
                Success = true,
                Data = updatedImage,
                Message = $"Image {id} set as primary successfully."
            });
        }
    }
}