using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Roles;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        // GET: api/Roles
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<RoleDto>>>> GetRoles()
        {
            var response = await _roleService.GetAllRolesAsync();
            return Ok(response);
        }

        // GET: api/Roles/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<RoleDto>>> GetRole(string id)
        {
            var response = await _roleService.GetRoleByIdAsync(id);
            if (!response.Succeeded) return NotFound(response);
            return Ok(response);
        }

        // POST: api/Roles (Tạo Role Custom)
        [HttpPost]
        public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole([FromBody] RoleCreateDto dto)
        {
            var response = await _roleService.CreateRoleAsync(dto);
            if (!response.Succeeded) return BadRequest(response);
            return CreatedAtAction(nameof(GetRole), new { id = response.Data!.RoleId }, response);
        }

        // PUT: api/Roles/{id} (Sửa Role Custom)
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse>> UpdateRole(string id, [FromBody] RoleUpdateDto dto)
        {
            var response = await _roleService.UpdateRoleAsync(id, dto);
            if (!response.Succeeded)
            {
                if (response.Message == ApiResponse.SM01_NO_RESULTS) return NotFound(response);
                return BadRequest(response);
            }
            return Ok(response);
        }

        // DELETE: api/Roles/{id} (Xóa mềm Role Custom)
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> DeleteRole(string id)
        {
            var response = await _roleService.DeleteRoleAsync(id);
            if (!response.Succeeded)
            {
                if (response.Message == ApiResponse.SM01_NO_RESULTS) return NotFound(response);
                return BadRequest(response);
            }
            return Ok(response);
        }

        // --- Quản lý Permissions ---

        // GET: api/Roles/permissions (Lấy tất cả permission)
        [HttpGet("permissions")]
        public async Task<ActionResult<ApiResponse<IEnumerable<PermissionDto>>>> GetAllPermissions()
        {
            var response = await _roleService.GetAllPermissionsAsync();
            return Ok(response);
        }

        // GET: api/Roles/{id}/permissions (Lấy permission của 1 role)
        [HttpGet("{id}/permissions")]
        public async Task<ActionResult<ApiResponse<IEnumerable<PermissionDto>>>> GetPermissionsForRole(string id)
        {
            var response = await _roleService.GetPermissionsForRoleAsync(id);
            if (!response.Succeeded) return NotFound(response);
            return Ok(response);
        }

        // PUT: api/Roles/{id}/permissions (Gán permission cho 1 role)
        [HttpPut("{id}/permissions")]
        public async Task<ActionResult<ApiResponse>> AssignPermissionsToRole(string id, [FromBody] PermissionAssignmentDto dto)
        {
            var response = await _roleService.AssignPermissionsToRoleAsync(id, dto);
            if (!response.Succeeded)
            {
                if (response.Message == ApiResponse.SM01_NO_RESULTS) return NotFound(response);
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}