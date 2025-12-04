using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Roles;
using ApartaAPI.DTOs.User;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserManagementController : ControllerBase
    {
        private readonly IUserManagementService _userService;
        private readonly IRoleService _roleService;

        public UserManagementController(IUserManagementService userService, IRoleService roleService)
        {
            _userService = userService;
            _roleService = roleService;
        }

        // GET: api/UserManagement/roles
        [HttpGet("roles")]
        public async Task<ActionResult<ApiResponse<IEnumerable<RoleDto>>>> GetRoles()
        {
            var response = await _roleService.GetAllRolesAsync();
            return Ok(response);
        }

        // 1. GET Staffs
        [HttpGet("staffs")]
        [Authorize(Policy = "ManagerAccess")]
        public async Task<ActionResult<ApiResponse<PagedList<UserAccountDto>>>> GetStaffs([FromQuery] UserQueryParams queryParams)
        {
            var response = await _userService.GetStaffAccountsAsync(queryParams);
            return Ok(response);
        }

        // 2. GET Residents
        [HttpGet("residents")]
        [Authorize(Policy = "ManagerAccess")]
        public async Task<ActionResult<ApiResponse<PagedList<UserAccountDto>>>> GetResidents([FromQuery] UserQueryParams queryParams)
        {
            var response = await _userService.GetResidentAccountsAsync(queryParams);
            return Ok(response);
        }

        // 3. CREATE Staff
        [HttpPost("staffs")]
        [Authorize(Policy = "ManagerAccess")]
        public async Task<ActionResult<ApiResponse<UserAccountDto>>> CreateStaff([FromBody] StaffCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<UserAccountDto>.Fail(ApiResponse.SM25_INVALID_INPUT));
            }

            var response = await _userService.CreateStaffAccountAsync(createDto);

            if (!response.Succeeded)
            {
                if (response.Message == ApiResponse.SM16_DUPLICATE_CODE)
                    return Conflict(response);

                return BadRequest(response);
            }

            return CreatedAtAction(nameof(GetStaffs), new { id = response.Data!.UserId }, response);
        }

        // 4. TOGGLE Status
        [HttpPut("{userId}/status")]
        [Authorize(Policy = "ManagerAccess")]
        public async Task<ActionResult<ApiResponse<UserAccountDto>>> ToggleStatus(string userId, [FromBody] StatusUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<UserAccountDto>.Fail(ApiResponse.SM25_INVALID_INPUT));

            var response = await _userService.ToggleUserStatusAsync(userId, dto);

            if (!response.Succeeded)
            {
                if (response.Message == ApiResponse.SM01_NO_RESULTS) return NotFound(response);
                return BadRequest(response);
            }
            return Ok(response);
        }

        // 5. UPDATE Assignments
        [HttpPut("staffs/{staffId}/assignments")]
        [Authorize(Policy = "ManagerAccess")]
        public async Task<ActionResult<ApiResponse>> UpdateStaffAssignments(string staffId, [FromBody] AssignmentUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Fail(ApiResponse.SM25_INVALID_INPUT));

            var response = await _userService.UpdateStaffAssignmentAsync(staffId, dto);

            if (!response.Succeeded)
            {
                if (response.Message == ApiResponse.SM01_NO_RESULTS) return NotFound(response);
                return StatusCode(500, response);
            }
            return Ok(response);
        }
        // Thêm vào class UserManagementController

        // 6. RESET PASSWORD (Admin/Manager force reset cho Staff)
        [HttpPost("staffs/{id}/reset-password")]
        [Authorize(Policy = "ManagerAccess")]
        public async Task<ActionResult<ApiResponse>> ResetStaffPassword(string id)
        {
            // Không cần validate ModelState vì không có Body
            var response = await _userService.ResetStaffPasswordAsync(id);

            if (!response.Succeeded)
            {
                if (response.Message == ApiResponse.SM01_NO_RESULTS) return NotFound(response);
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}