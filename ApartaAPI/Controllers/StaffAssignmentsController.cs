using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.StaffAssignments;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StaffAssignmentsController : ControllerBase
    {
        private readonly IStaffAssignmentService _service;
        private readonly IAuthorizationService _authorizationService;

        public StaffAssignmentsController(IStaffAssignmentService service, IAuthorizationService authorizationService)
        {
            _service = service;
            _authorizationService = authorizationService;
        }

        // Helper lấy UserID hiện tại
        private string GetCurrentUserId()
        {
            return User.FindFirstValue("id")
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? string.Empty;
        }

        // 1. Lấy danh sách phân công (Có phân trang, filter)
        [HttpGet]
        [Authorize(Policy = "CanReadStaffAssignment")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<StaffAssignmentDto>>>> GetAssignments(
            [FromQuery] StaffAssignmentQueryParameters query)
        {
            var response = await _service.GetAssignmentsAsync(query);
            return Ok(response);
        }

        // [MỚI] API lấy danh sách nhân viên để fill dropdown
        [HttpGet("available-staffs")]
        [Authorize(Policy = "CanReadStaffAssignment")]
        public async Task<ActionResult<ApiResponse<IEnumerable<StaffUserDto>>>> GetAvailableStaffs(
            [FromQuery] string? searchTerm)
        {
            var response = await _service.GetAvailableStaffsAsync(searchTerm);
            return Ok(response);
        }

        [HttpGet("available-buildings")]
        [Authorize(Policy = "CanReadStaffAssignment")]
        public async Task<ActionResult<ApiResponse<IEnumerable<StaffAssignmentBuildingDto>>>> GetAvailableBuildings(
            [FromQuery] string? searchTerm)
        {
            var response = await _service.GetAvailableBuildingsAsync(searchTerm);
            return Ok(response);
        }

        // 2. Gán nhân viên vào tòa nhà (Tạo mới)
        [HttpPost]
        [Authorize(Policy = "CanCreateStaffAssignment")]
        public async Task<ActionResult<ApiResponse<StaffAssignmentDto>>> AssignStaff(
            [FromBody] StaffAssignmentCreateDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<StaffAssignmentDto>.Fail(ApiResponse.SM25_INVALID_INPUT));
            }

            var managerId = GetCurrentUserId();
            var response = await _service.AssignStaffAsync(request, managerId);

            if (!response.Succeeded)
            {
                // Xử lý các mã lỗi cụ thể từ Service
                if (response.Message == ApiResponse.SM42_ASSIGNMENT_OVERLAP)
                    return Conflict(response); // 409 Conflict

                if (response.Message == ApiResponse.SM01_NO_RESULTS || response.Message == ApiResponse.SM29_USER_NOT_FOUND)
                    return NotFound(response);

                return BadRequest(response);
            }

            return CreatedAtAction(nameof(GetAssignments), new { id = response.Data!.AssignmentId }, response);
        }

        // 3. Cập nhật thông tin phân công (Vị trí, Scope, Trạng thái)
        [HttpPut("{id}")]
        [Authorize(Policy = "CanUpdateStaffAssignment")]
        public async Task<ActionResult<ApiResponse<StaffAssignmentDto>>> UpdateAssignment(
            string id,
            [FromBody] StaffAssignmentUpdateDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<StaffAssignmentDto>.Fail(ApiResponse.SM25_INVALID_INPUT));
            }

            var managerId = GetCurrentUserId();
            var response = await _service.UpdateAssignmentAsync(id, request, managerId);

            if (!response.Succeeded)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        // 4. Xóa phân công
        // Policy "CanUpdateStaffAssignment" đảm bảo User là Admin HOẶC Manager HOẶC có quyền update
        [HttpDelete("{id}")]
        [Authorize(Policy = "CanUpdateStaffAssignment")]
        public async Task<ActionResult<ApiResponse>> DeleteAssignment(string id)
        {
            ApiResponse response;

            // Logic phân loại xóa dựa trên Role
            if (User.IsInRole("admin"))
            {
                // ADMIN: Xóa cứng (Xóa vĩnh viễn khỏi DB)
                response = await _service.DeletePermanentAsync(id);
            }
            else
            {
                // MANAGER: Xóa mềm (Chuyển trạng thái nghỉ việc/Inactive)
                response = await _service.DeactivateAssignmentAsync(id);
            }

            if (!response.Succeeded)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
    }
}