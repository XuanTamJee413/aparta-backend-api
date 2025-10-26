using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.News;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Policy = "StaffOrAdmin")]
    public class NewsController : ControllerBase
    {
        private readonly INewsService _newsService;

        public NewsController(INewsService newsService)
        {
            _newsService = newsService;
        }

        // GET: api/News - News list với search và filter theo status
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<NewsDto>>), 200)]
        public async Task<ActionResult<ApiResponse<IEnumerable<NewsDto>>>> GetAllNews(
            [FromQuery] string? searchTerm,
            [FromQuery] string? status) 
        {
            var query = new NewsSearchDto(searchTerm, status);
            var response = await _newsService.GetAllNewsAsync(query);
            return Ok(response);
        }

        // POST: api/News - News mới
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<NewsDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<NewsDto>), 400)]
        public async Task<ActionResult<ApiResponse<NewsDto>>> CreateNews([FromBody] CreateNewsDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                
                return BadRequest(ApiResponse<NewsDto>.Fail(errors));
            }

            // Lấy userId từ token, nếu không có thì dùng hardcode để test
            var userId = User.FindFirst("id")?.Value;
            
            // TEMPORARY: Hardcode authorUserId để test (vì đang lỗi authorize)
            if (string.IsNullOrEmpty(userId))
            {
                userId = "BD0BC556-622E-4393-8B9D-A94EA922E6AD"; // TODO: Remove after fixing auth
            }

            var response = await _newsService.CreateNewsAsync(request, userId);
            
            if (!response.Succeeded)
            {
                return BadRequest(response);
            }

            return CreatedAtAction(nameof(GetAllNews), new { id = response.Data!.NewsId }, response);
        }

        // PUT: api/News/{id} - Sửa news
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<NewsDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<NewsDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<NewsDto>), 404)]
        public async Task<ActionResult<ApiResponse<NewsDto>>> UpdateNews(string id, [FromBody] UpdateNewsDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                
                return BadRequest(ApiResponse<NewsDto>.Fail(errors));
            }

            var response = await _newsService.UpdateNewsAsync(id, request);
            
            if (!response.Succeeded)
            {
                if (response.Message == ApiResponse.SM01_NO_RESULTS)
                {
                    return NotFound(response);
                }
                return BadRequest(response);
            }

            return Ok(response);
        }

        // DELETE: api/News/{id} - Xóa news
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse>> DeleteNews(string id)
        {
            var response = await _newsService.DeleteNewsAsync(id);
            
            if (!response.Succeeded)
            {
                if (response.Message == ApiResponse.SM01_NO_RESULTS)
                {
                    return NotFound(response);
                }
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}

