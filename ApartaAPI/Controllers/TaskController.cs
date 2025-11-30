using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Tasks;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ApartaAPI.Controllers
{
	[Route("api/tasks")]
	[ApiController]
	[Authorize]
	public class TaskController : ControllerBase
	{
		private readonly ITaskService _taskService;

		public TaskController(ITaskService taskService)
		{
			_taskService = taskService;
		}

		private string GetCurrentUserId()
		{
			return User.FindFirstValue("id")
				?? User.FindFirstValue(ClaimTypes.NameIdentifier)
				?? User.FindFirstValue("sub")
				?? throw new UnauthorizedAccessException("User ID not found.");
		}

		// 1. Lấy danh sách Task (Dành cho Operation Staff quản lý)
		[HttpGet]
		public async Task<ActionResult<PagedList<TaskDto>>> GetAllTasks([FromQuery] TaskQueryParameters parameters)
		{
			var result = await _taskService.GetAllTasksAsync(parameters);
			return Ok(result);
		}

		// 2. Lấy danh sách Task của tôi (Dành cho Maintenance Staff)
		[HttpGet("my")]
		public async Task<ActionResult<PagedList<TaskDto>>> GetMyTasks([FromQuery] TaskQueryParameters parameters)
		{
			var userId = GetCurrentUserId();
			var result = await _taskService.GetMyTasksAsync(userId, parameters);
			return Ok(result);
		}

		// 3. Tạo Task mới
		[HttpPost]
		public async Task<ActionResult<TaskDto>> CreateTask([FromBody] TaskCreateDto createDto)
		{
			var userId = GetCurrentUserId();
			var result = await _taskService.CreateTaskAsync(createDto, userId);
			return CreatedAtAction(nameof(GetTaskById), new { id = result.TaskId }, result);
		}

		// 4. Phân công Task (Assign)
		[HttpPost("assign")]
		public async Task<IActionResult> AssignTask([FromBody] TaskAssignmentCreateDto assignmentDto)
		{
			var assignerId = GetCurrentUserId();
			try
			{
				var result = await _taskService.AssignTaskAsync(assignmentDto, assignerId);
				if (result) return Ok(new { message = "Phân công thành công." });
				return BadRequest("Không thể phân công.");
			}
			catch (ArgumentException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		// 5. Cập nhật trạng thái (Hoàn thành/Hủy)
		[HttpPut("{id}/status")]
		public async Task<IActionResult> UpdateStatus(string id, [FromBody] TaskUpdateStatusDto updateDto)
		{
			var result = await _taskService.UpdateTaskStatusAsync(id, updateDto);
			if (result == null) return NotFound();
			return Ok(result);
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<TaskDto>> GetTaskById(string id)
		{
			var result = await _taskService.GetTaskByIdAsync(id);
			if (result == null) return NotFound();
			return Ok(result);
		}

		// POST: api/tasks/unassign
		[HttpPost("unassign")]
		public async Task<IActionResult> UnassignTask([FromBody] TaskUnassignDto unassignDto)
		{
			try
			{
				// Bạn cần thêm UnassignTaskAsync vào Interface ITaskService trước nhé
				var result = await _taskService.UnassignTaskAsync(unassignDto.TaskId, unassignDto.AssigneeUserId);
				if (result) return Ok(new { message = "Đã gỡ nhân viên khỏi task." });
				return BadRequest("Lỗi khi gỡ nhân viên.");
			}
			catch (ArgumentException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}
	}
}