using ApartaAPI.DTOs.ApartmentMembers;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class ManagerController : ControllerBase
    {
        private readonly IManagerService _managerService;

        public ManagerController(IManagerService managerService)
        {
            _managerService = managerService;
        }

        // GET: api/Manager - Lấy danh sách managers với search
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ManagerDto>>), 200)]
        public async Task<ActionResult<ApiResponse<IEnumerable<ManagerDto>>>> GetAllManagers([FromQuery] string? searchTerm)
        {
            var query = new ManagerSearch(searchTerm);
            var response = await _managerService.GetAllManagersAsync(query);
            return Ok(response);
        }

        // POST: api/Manager - tao manager mới
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ManagerDto>>> CreateManager([FromBody] CreateManagerDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                
                return BadRequest(ApiResponse<ManagerDto>.Fail(errors));
            }

            var response = await _managerService.CreateManagerAsync(request);
            
            if (!response.Succeeded)
            {
                return BadRequest(response);
            }

            return CreatedAtAction(nameof(GetAllManagers), new { id = response.Data!.UserId }, response);
        }

        // PUT: api/Manager/{id} - cập nhật thông tin manager
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<ManagerDto>>> UpdateManager(string id, [FromBody] UpdateManagerDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return BadRequest(ApiResponse<ManagerDto>.Fail(errors));
            }

            var response = await _managerService.UpdateManagerAsync(id, request);

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
