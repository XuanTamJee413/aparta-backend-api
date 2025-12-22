using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.News;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsController : ControllerBase
    {
        private readonly INewsService _newsService;

        public NewsController(INewsService newsService)
        {
            _newsService = newsService;
        }

        // GET: api/News - News list với search và filter theo status
        [HttpGet]
        [Authorize(Policy = "CanReadNews")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<NewsDto>>), 200)]
        public async Task<ActionResult<ApiResponse<IEnumerable<NewsDto>>>> GetAllNews(
            [FromQuery] string? searchTerm,
            [FromQuery] string? status) 
        {
            try
            {
                var userId = User.FindFirst("id")?.Value ??
                             User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<IEnumerable<NewsDto>>.Fail("User ID not found in token. Please login again."));
                }

                var query = new NewsSearchDto(searchTerm, status);
                var response = await _newsService.GetAllNewsAsync(query, userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<IEnumerable<NewsDto>>.Fail($"An error occurred while loading news: {ex.Message}"));
            }
        }

        // POST: api/News - News mới
        [HttpPost]
        [Authorize(Policy = "CanCreateNews")]
        [ProducesResponseType(typeof(ApiResponse<NewsDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<NewsDto>), 400)]
        public async Task<ActionResult<ApiResponse<NewsDto>>> CreateNews([FromBody] CreateNewsDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    
                    return BadRequest(ApiResponse<NewsDto>.Fail(errors));
                }

                // Lấy userId từ token JWT
                var userId = User.FindFirst("id")?.Value ?? 
                             User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<NewsDto>.Fail("User ID not found in token. Please login again."));
                }

                var response = await _newsService.CreateNewsAsync(request, userId);
                
                if (!response.Succeeded)
                {
                    return BadRequest(response);
                }

                return CreatedAtAction(nameof(GetAllNews), new { id = response.Data!.NewsId }, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<NewsDto>.Fail($"An error occurred while creating news: {ex.Message}"));
            }
        }

        // PUT: api/News/{id} - Sửa news
        [HttpPut("{id}")]
        [Authorize(Policy = "CanUpdateNews")]
        [ProducesResponseType(typeof(ApiResponse<NewsDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<NewsDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<NewsDto>), 404)]
        public async Task<ActionResult<ApiResponse<NewsDto>>> UpdateNews(string id, [FromBody] UpdateNewsDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    
                    return BadRequest(ApiResponse<NewsDto>.Fail(errors));
                }

                // Lấy userId từ token JWT
                var userId = User.FindFirst("id")?.Value ?? 
                             User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<NewsDto>.Fail("User ID not found in token. Please login again."));
                }

                var response = await _newsService.UpdateNewsAsync(id, request, userId);
                
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
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<NewsDto>.Fail($"An error occurred while updating news: {ex.Message}"));
            }
        }

        // DELETE: api/News/{id} - Xóa news
        [HttpDelete("{id}")]
        [Authorize(Policy = "CanDeleteNews")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse>> DeleteNews(string id)
        {
            try
            {
                // Lấy userId từ token JWT
                var userId = User.FindFirst("id")?.Value ?? 
                             User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse.Fail("User ID not found in token. Please login again."));
                }

                var response = await _newsService.DeleteNewsAsync(id, userId);
                
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
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.Fail($"An error occurred while deleting news: {ex.Message}"));
            }
        }
    }
}

