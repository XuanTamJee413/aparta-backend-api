/* --- File: Controllers/UserManagementController.cs --- */

using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.User;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserManagementController : ControllerBase
    {
        private readonly IUserManagementService _userService;
        public UserManagementController(IUserManagementService userService)
        {
            _userService = userService;
        }

        // ===================================
        // 1. STAFF MANAGEMENT (LẤY DANH SÁCH)
        // GET: api/UserManagement/staffs?pageNumber=1...
        // ===================================
        [HttpGet("staffs")]
        public async Task<ActionResult<ApiResponse<PagedList<UserAccountDto>>>> GetStaffs([FromQuery] UserQueryParams queryParams)
        {
            var result = await _userService.GetStaffAccountsAsync(queryParams);
            // Service đã trả về ApiResponse<PagedList<T>>, chỉ cần Ok(result)
            return Ok(result);
        }

        // ===================================
        // 2. RESIDENT MANAGEMENT (LẤY DANH SÁCH)
        // GET: api/UserManagement/residents?pageNumber=1...
        // ===================================
        [HttpGet("residents")]
        public async Task<ActionResult<ApiResponse<PagedList<UserAccountDto>>>> GetResidents([FromQuery] UserQueryParams queryParams)
        {
            var result = await _userService.GetResidentAccountsAsync(queryParams);
            return Ok(result);
        }

        // ===================================
        // 3. TẠO STAFF MỚI
        // POST: api/UserManagement/staffs
        // ===================================
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

                // Trả về 201 Created với ApiResponse thành công
                return CreatedAtAction(
                    nameof(GetStaffs),
                    new { id = newUser.UserId },
                    ApiResponse<UserAccountDto>.SuccessWithCode(newUser, ApiResponse.SM04_CREATE_SUCCESS, "User"));
            }
            catch (InvalidOperationException ex)
            {
                // Xử lý lỗi trùng lặp (Email/Phone) từ Service
                if (ex.Message.Contains("Email đã tồn tại."))
                {
                    return Conflict(ApiResponse<UserAccountDto>.Fail(ApiResponse.SM16_DUPLICATE_CODE, "Email"));
                }
                if (ex.Message.Contains("Số điện thoại đã tồn tại."))
                {
                    return Conflict(ApiResponse<UserAccountDto>.Fail(ApiResponse.SM16_DUPLICATE_CODE, "Phone"));
                }
                return BadRequest(ApiResponse<UserAccountDto>.Fail(ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<UserAccountDto>.Fail(ex.Message));
            }
        }

        // ===================================
        // 4. ACTIVE/DEACTIVE TÀI KHOẢN (Chung cho Staff/Resident)
        // PUT: api/UserManagement/{userId}/status
        // ===================================
        [HttpPut("{userId}/status")]
        public async Task<ActionResult<ApiResponse<UserAccountDto>>> ToggleStatus(string userId, [FromBody] StatusUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<UserAccountDto>.Fail(ApiResponse.SM25_INVALID_INPUT));
            }
            try
            {
                // Service trả về UserAccountDto, Controller bọc lại
                var updatedUser = await _userService.ToggleUserStatusAsync(userId, dto);

                if (updatedUser == null) return NotFound(ApiResponse<UserAccountDto>.Fail(ApiResponse.SM01_NO_RESULTS));

                return Ok(ApiResponse<UserAccountDto>.Success(updatedUser, ApiResponse.SM03_UPDATE_SUCCESS));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponse<UserAccountDto>.Fail(ApiResponse.SM01_NO_RESULTS));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<UserAccountDto>.Fail(ex.Message));
            }
        }

        // ===================================
        // 5. CHUYỂN VỊ TRÍ LÀM VIỆC (Staff)
        // PUT: api/UserManagement/staffs/{staffId}/assignments
        // ===================================
        [HttpPut("staffs/{staffId}/assignments")]
        public async Task<ActionResult<ApiResponse>> UpdateStaffAssignments(string staffId, [FromBody] AssignmentUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail(ApiResponse.SM25_INVALID_INPUT));
            }

            try
            {
                await _userService.UpdateStaffAssignmentAsync(staffId, dto);

                // Trả về OK với thông báo thành công chuẩn hóa
                return Ok(ApiResponse.Success(ApiResponse.SM03_UPDATE_SUCCESS));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.Fail($"Lỗi: {ex.Message}")); // Dùng NotFound nếu là lỗi ID không tồn tại
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse.Fail($"Lỗi: {ex.Message}"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.Fail($"Lỗi hệ thống: {ex.Message}"));
            }
        }
    }
}