using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Tasks;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApartaAPI.Services
{
	public class TaskService : ITaskService
	{
		private readonly IRepository<ApartaAPI.Models.Task> _taskRepository;
		private readonly IRepository<TaskAssignment> _assignmentRepository;
		private readonly IRepository<User> _userRepository;
		private readonly IRepository<Role> _roleRepository;

		public TaskService(
			IRepository<ApartaAPI.Models.Task> taskRepository,
			IRepository<TaskAssignment> assignmentRepository,
			IRepository<User> userRepository,
			IRepository<Role> roleRepository)
		{
			_taskRepository = taskRepository;
			_assignmentRepository = assignmentRepository;
			_userRepository = userRepository;
			_roleRepository = roleRepository;
		}

		// 1. Lấy tất cả Task (Dành cho Operation Staff)
		public async Task<PagedList<TaskDto>> GetAllTasksAsync(TaskQueryParameters parameters)
		{
			var tasks = await _taskRepository.GetAllAsync();
			var assignments = await _assignmentRepository.GetAllAsync();
			var users = await _userRepository.GetAllAsync();

			// Join dữ liệu để lấy thông tin Assignee mới nhất
			var query = tasks.Select(t => MapToDto(t, assignments, users)).AsQueryable();

			// Lọc
			if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
			{
				string search = parameters.SearchTerm.Trim().ToLower();
				query = query.Where(t => t.Description.ToLower().Contains(search) || t.Type.ToLower().Contains(search));
			}
			if (!string.IsNullOrWhiteSpace(parameters.Status))
			{
				query = query.Where(t => t.Status == parameters.Status);
			}

			// Phân trang
			var count = query.Count();
			var items = query
				.OrderByDescending(t => t.CreatedAt)
				.Skip((parameters.PageNumber - 1) * parameters.PageSize)
				.Take(parameters.PageSize)
				.ToList();

			return new PagedList<TaskDto>(items, count, parameters.PageNumber, parameters.PageSize);
		}

		// 2. Tạo Task Mới
		public async Task<TaskDto> CreateTaskAsync(TaskCreateDto createDto, string operationStaffId)
		{
			var newTask = new ApartaAPI.Models.Task
			{
				TaskId = Guid.NewGuid().ToString(),
				ServiceBookingId = createDto.ServiceBookingId,
				OperationStaffId = operationStaffId,
				Type = createDto.Type,
				Description = createDto.Description,
				Status = "New",
				StartDate = createDto.StartDate,
				EndDate = createDto.EndDate,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			await _taskRepository.AddAsync(newTask);
			await _taskRepository.SaveChangesAsync();

			// Khi mới tạo chưa có assignment
			var creator = await _userRepository.FirstOrDefaultAsync(u => u.UserId == operationStaffId);
			return new TaskDto(
				newTask.TaskId, newTask.ServiceBookingId, newTask.OperationStaffId,
				creator?.Name ?? "Unknown", newTask.Type, newTask.Description,
				newTask.Status, newTask.StartDate, newTask.EndDate, newTask.CreatedAt,
				null, null, null
			);
		}

		// 3. Phân công Task (Quan trọng)
		public async Task<bool> AssignTaskAsync(TaskAssignmentCreateDto assignmentDto, string assignerId)
		{
			var task = await _taskRepository.FirstOrDefaultAsync(t => t.TaskId == assignmentDto.TaskId);
			if (task == null) throw new ArgumentException("Task không tồn tại.");

			// Lấy thông tin người được giao
			var assignee = await _userRepository.FirstOrDefaultAsync(u => u.UserId == assignmentDto.AssigneeUserId);
			if (assignee == null) throw new ArgumentException("Nhân viên không tồn tại.");

			// --- SỬA LỖI TẠI ĐÂY ---
			// Thay vì gọi assignee.Role.RoleName (bị null), hãy tìm Role trong bảng Role
			var role = await _roleRepository.FirstOrDefaultAsync(r => r.RoleId == assignee.RoleId);

			// Kiểm tra xem Role có tồn tại và có phải là 'maintenance_staff' không
			// Lưu ý: Đảm bảo chuỗi "maintenance_staff" khớp chính xác với DB của bạn
			if (role == null || role.RoleName != "maintenance_staff")
			{
				throw new ArgumentException("Người được giao việc phải là nhân viên bảo trì (maintenance_staff).");
			}

			// Tạo Assignment mới
			var assignment = new TaskAssignment
			{
				TaskAssignmentId = Guid.NewGuid().ToString(),
				TaskId = assignmentDto.TaskId,
				AssignerUserId = assignerId,
				AssigneeUserId = assignmentDto.AssigneeUserId,
				AssignedDate = DateTime.UtcNow,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			await _assignmentRepository.AddAsync(assignment);

			// Cập nhật trạng thái Task
			task.Status = "Assigned";
			task.UpdatedAt = DateTime.UtcNow;
			await _taskRepository.UpdateAsync(task);

			await _taskRepository.SaveChangesAsync();

			return true;
		}

		// 4. Lấy Task của tôi (Dành cho Maintenance Staff)
		public async Task<PagedList<TaskDto>> GetMyTasksAsync(string assigneeId, TaskQueryParameters parameters)
		{
			// Logic: Tìm trong bảng Assignment những dòng có AssigneeUserId == assigneeId
			var myAssignments = (await _assignmentRepository.GetAllAsync())
								.Where(a => a.AssigneeUserId == assigneeId)
								.Select(a => a.TaskId)
								.Distinct();

			var allTasks = await _taskRepository.GetAllAsync();
			var users = await _userRepository.GetAllAsync(); // Để map tên
			var allAssignments = await _assignmentRepository.GetAllAsync(); // Để map assignment info

			var myTasks = allTasks.Where(t => myAssignments.Contains(t.TaskId));

			var query = myTasks.Select(t => MapToDto(t, allAssignments, users)).AsQueryable();

			if (!string.IsNullOrWhiteSpace(parameters.Status))
			{
				query = query.Where(t => t.Status == parameters.Status);
			}

			var count = query.Count();
			var items = query
				.OrderByDescending(t => t.CreatedAt)
				.Skip((parameters.PageNumber - 1) * parameters.PageSize)
				.Take(parameters.PageSize)
				.ToList();

			return new PagedList<TaskDto>(items, count, parameters.PageNumber, parameters.PageSize);
		}

		// 5. Cập nhật trạng thái (Hoàn thành/Hủy)
		public async Task<TaskDto?> UpdateTaskStatusAsync(string taskId, TaskUpdateStatusDto updateDto)
		{
			var task = await _taskRepository.FirstOrDefaultAsync(t => t.TaskId == taskId);
			if (task == null) return null;

			task.Status = updateDto.Status;
			// Nếu có note, bạn có thể lưu vào Description hoặc một trường Note riêng nếu có
			if (!string.IsNullOrEmpty(updateDto.Note))
			{
				task.Description += $" [Update: {updateDto.Note}]";
			}
			task.UpdatedAt = DateTime.UtcNow;

			await _taskRepository.UpdateAsync(task);
			await _taskRepository.SaveChangesAsync();

			// Lấy data để return
			var assignments = await _assignmentRepository.GetAllAsync();
			var users = await _userRepository.GetAllAsync();
			return MapToDto(task, assignments, users);
		}

		public async Task<TaskDto?> GetTaskByIdAsync(string id)
		{
			var task = await _taskRepository.FirstOrDefaultAsync(t => t.TaskId == id);
			if (task == null) return null;
			var assignments = await _assignmentRepository.GetAllAsync();
			var users = await _userRepository.GetAllAsync();
			return MapToDto(task, assignments, users);
		}

		// Helper Mapping
		private TaskDto MapToDto(ApartaAPI.Models.Task task, IEnumerable<TaskAssignment> assignments, IEnumerable<User> users)
		{
			var creator = users.FirstOrDefault(u => u.UserId == task.OperationStaffId);

			// Lấy assignment mới nhất cho task này
			var latestAssignment = assignments
				.Where(a => a.TaskId == task.TaskId)
				.OrderByDescending(a => a.AssignedDate)
				.FirstOrDefault();

			string? assigneeId = latestAssignment?.AssigneeUserId;
			string? assigneeName = assigneeId != null ? users.FirstOrDefault(u => u.UserId == assigneeId)?.Name : null;

			return new TaskDto(
				task.TaskId,
				task.ServiceBookingId,
				task.OperationStaffId,
				creator?.Name ?? "Unknown",
				task.Type,
				task.Description,
				task.Status,
				task.StartDate,
				task.EndDate,
				task.CreatedAt,
				assigneeId,
				assigneeName,
				latestAssignment?.AssignedDate
			);
		}
	}
}