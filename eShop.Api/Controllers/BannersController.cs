using eShop.Core.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using eShop.Core.Models;
using eShop.Core.DTOs;
using AutoMapper;

namespace eShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BannersController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public BannersController(IUnitOfWork unitOfWork, IFileService fileService, IMapper mapper)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        // GET: api/banners
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BannerResponseDto>>> GetBanners()
        {
            var banners = await _unitOfWork.BannerRepository
                .FindAllAsync(b => (b.EndDate == null || b.EndDate > DateTime.UtcNow));
            var bannerDtos = _mapper.Map<IEnumerable<BannerResponseDto>>(banners);
            return Ok(bannerDtos);
        }

        // GET: api/banners/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<BannerResponseDto>> GetBanner(int id)
        {
            var banner = await _unitOfWork.BannerRepository.GetByIdAsync(id);
            if (banner == null)
            {
                return NotFound();
            }
            var bannerDto = _mapper.Map<BannerResponseDto>(banner);
            return Ok(bannerDto);
        }

        // POST: api/banners
        [HttpPost]
        public async Task<ActionResult<BannerResponseDto>> CreateBanner([FromForm] BannerCreateDto bannerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                using var transaction = _unitOfWork.BeginTransaction();

                var banner = _mapper.Map<Banner>(bannerDto);

                // Handle image upload
                if (bannerDto.Image != null)
                {
                    banner.ImageUrl = await _fileService.SaveFileAsync(bannerDto.Image, "banners");
                }

                await _unitOfWork.BannerRepository.AddAsync(banner);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var responseDto = _mapper.Map<BannerResponseDto>(banner);
                return CreatedAtAction(nameof(GetBanner), new { id = banner.Id }, responseDto);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // PUT: api/banners/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBanner(int id, [FromForm] BannerUpdateDto bannerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != bannerDto.Id)
            {
                return BadRequest("Banner ID mismatch");
            }

            var existingBanner = await _unitOfWork.BannerRepository.GetByIdAsync(id);
            if (existingBanner == null)
            {
                return NotFound();
            }

            try
            {
                using var transaction = _unitOfWork.BeginTransaction();

                // Update banner properties using AutoMapper
                _mapper.Map(bannerDto, existingBanner);

                // Handle image update if provided
                if (bannerDto.Image != null)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(existingBanner.ImageUrl))
                    {
                        await _fileService.DeleteFileAsync(existingBanner.ImageUrl);
                    }
                    existingBanner.ImageUrl = await _fileService.SaveFileAsync(bannerDto.Image, "banners");
                }

                await _unitOfWork.BannerRepository.UpdateAsync(existingBanner);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // DELETE: api/banners/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBanner(int id)
        {
            var banner = await _unitOfWork.BannerRepository.GetByIdAsync(id);
            if (banner == null)
            {
                return NotFound();
            }

            try
            {
                using var transaction = _unitOfWork.BeginTransaction();

                // Delete associated image
                if (!string.IsNullOrEmpty(banner.ImageUrl))
                {
                    await _fileService.DeleteFileAsync(banner.ImageUrl);
                }

                await _unitOfWork.BannerRepository.RemoveAsync(banner);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}