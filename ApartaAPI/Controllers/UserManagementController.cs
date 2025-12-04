using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Roles;
using ApartaAPI.DTOs.User;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization; // [MỚI]
using Microsoft.AspNetCore.Mvc;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // [MỚI] Yêu cầu đăng nhập mặc định
    public class UserManagementController : ControllerBase
    {
        private readonly IUserManagementService _userService;
        private readonly IRoleService _roleService;

        public UserManagementController(IUserManagementService userService, IRoleService roleService)
        {
            _userService = userService;
            _roleService = roleService;
        }

        // GET: api/Roles
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<RoleDto>>>> GetRoles()
        {
            var response = await _roleService.GetAllRolesAsync();
            return Ok(response);
        }
        // 1. GET Staffs - Chỉ Admin được xem
        [HttpGet("staffs")]
        [Authorize(Policy = "ManagerAccess")]
        public async Task<ActionResult<ApiResponse<PagedList<UserAccountDto>>>> GetStaffs([FromQuery] UserQueryParams queryParams)
        {
            var result = await _userService.GetStaffAccountsAsync(queryParams);
            return Ok(result);
        }

        // 2. GET Residents - Admin và Manager được xem
        [HttpGet("residents")]
        [Authorize(Policy = "ManagerAccess")]
        public async Task<ActionResult<ApiResponse<PagedList<UserAccountDto>>>> GetResidents([FromQuery] UserQueryParams queryParams)
        {
            var result = await _userService.GetResidentAccountsAsync(queryParams);
            return Ok(result);
        }

        // 3. CREATE Staff - Chỉ Manager tạo
        [HttpPost("staffs")]
        public async Task<ActionResult<ApiResponse<UserAccountDto>>> CreateStaff([FromBody] StaffCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<UserAccountDto>.Fail(ApiResponse.SM25_INVALID_INPUT));
            }
            try
            {
                var newUser = await _userService.CreateStaffAccountAsync(createDto);

                return CreatedAtAction(
                    nameof(GetStaffs),
                    new { id = newUser.UserId },
                    ApiResponse<UserAccountDto>.SuccessWithCode(newUser, ApiResponse.SM04_CREATE_SUCCESS, "Nhân viên"));
            }
            catch (InvalidOperationException ex)
            {
                // Xử lý lỗi logic (trùng email/phone)
                return Conflict(ApiResponse<UserAccountDto>.Fail(ApiResponse.SM16_DUPLICATE_CODE, null, ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<UserAccountDto>.Fail(ApiResponse.SM25_INVALID_INPUT, null, ex.Message));
            }
        }

        // 4. TOGGLE Status - Admin/Manager quản lý
        [HttpPut("{userId}/status")]
        [Authorize(Policy = "ManagerAccess")]
        public async Task<ActionResult<ApiResponse<UserAccountDto>>> ToggleStatus(string userId, [FromBody] StatusUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<UserAccountDto>.Fail(ApiResponse.SM25_INVALID_INPUT));
            }
            try
            {
                var updatedUser = await _userService.ToggleUserStatusAsync(userId, dto);
                return Ok(ApiResponse<UserAccountDto>.Success(updatedUser, ApiResponse.SM03_UPDATE_SUCCESS));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<UserAccountDto>.Fail(ApiResponse.SM01_NO_RESULTS, null, ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<UserAccountDto>.Fail(ApiResponse.SM25_INVALID_INPUT, null, ex.Message));
            }
        }

        // 5. UPDATE Assignments - Chỉ Manager điều phối nhân viên
        [HttpPut("staffs/{staffId}/assignments")]
        [Authorize(Policy = "ManagerAccess")]
        public async Task<ActionResult<ApiResponse>> UpdateStaffAssignments(string staffId, [FromBody] AssignmentUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail(ApiResponse.SM25_INVALID_INPUT));
            }

            try
            {
                await _userService.UpdateStaffAssignmentAsync(staffId, dto);
                return Ok(ApiResponse.Success(ApiResponse.SM06_TASK_ASSIGN_SUCCESS));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS, null, ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.Fail(ApiResponse.SM40_SYSTEM_ERROR, null, ex.Message));
            }
        }
    }
}