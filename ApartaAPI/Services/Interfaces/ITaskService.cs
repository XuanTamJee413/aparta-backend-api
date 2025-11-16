using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApartaAPI.Services.Interfaces
{
	public interface ITaskService
	{
		// Quản lý Task
		Task<PagedList<TaskDto>> GetAllTasksAsync(TaskQueryParameters parameters);
		Task<PagedList<TaskDto>> GetMyTasksAsync(string assigneeId, TaskQueryParameters parameters); // Cho Maintenance Staff
		Task<TaskDto?> GetTaskByIdAsync(string id);
		Task<TaskDto> CreateTaskAsync(TaskCreateDto createDto, string operationStaffId);

		// Phân công
		Task<bool> AssignTaskAsync(TaskAssignmentCreateDto assignmentDto, string assignerId);

		// Cập nhật trạng thái
		Task<TaskDto?> UpdateTaskStatusAsync(string taskId, TaskUpdateStatusDto updateDto);
	}
}