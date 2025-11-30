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

		public async Task<PagedList<TaskDto>> GetAllTasksAsync(TaskQueryParameters parameters)
		{
			var tasks = await _taskRepository.GetAllAsync();
			var assignments = await _assignmentRepository.GetAllAsync();
			var users = await _userRepository.GetAllAsync();

			var query = tasks.Select(t => MapToDto(t, assignments, users)).AsQueryable();

			if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
			{
				string search = parameters.SearchTerm.Trim().ToLower();
				query = query.Where(t => t.Description.ToLower().Contains(search) || t.Type.ToLower().Contains(search));
			}
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

		// 2. Tạo Task Mới
		public async Task<TaskDto> CreateTaskAsync(TaskCreateDto createDto, string operationStaffId)
		{

			if (createDto.StartDate.HasValue && createDto.StartDate.Value < DateTime.UtcNow.AddMinutes(-1))
			{
				throw new ArgumentException("Thời gian bắt đầu không thể ở trong quá khứ.");
			}

			// 2. Kiểm tra ngày kết thúc phải sau ngày bắt đầu
			if (createDto.StartDate.HasValue && createDto.EndDate.HasValue)
			{
				if (createDto.EndDate.Value <= createDto.StartDate.Value)
				{
					throw new ArgumentException("Thời gian kết thúc phải sau thời gian bắt đầu.");
				}
			}
			else if (!createDto.StartDate.HasValue && createDto.EndDate.HasValue)
			{
				// Trường hợp không nhập StartDate (tức là bắt đầu ngay bây giờ)
				// thì EndDate phải lớn hơn hiện tại
				if (createDto.EndDate.Value <= DateTime.UtcNow)
				{
					throw new ArgumentException("Thời gian kết thúc phải lớn hơn thời gian hiện tại.");
				}
			}

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
				UpdatedAt = DateTime.UtcNow,
				AssigneeNote = null
			};

			await _taskRepository.AddAsync(newTask);
			await _taskRepository.SaveChangesAsync();

			var creator = await _userRepository.FirstOrDefaultAsync(u => u.UserId == operationStaffId);

			// Trả về DTO với danh sách Assignees rỗng
			return new TaskDto(
				newTask.TaskId, newTask.ServiceBookingId, newTask.OperationStaffId,
				creator?.Name ?? "Unknown", newTask.Type, newTask.Description,
				newTask.Status, newTask.StartDate, newTask.EndDate, newTask.CreatedAt,
				new List<TaskAssigneeDto>(), // <-- List rỗng
				null, // AssignedDate
				null  // Note
			);
		}

		// 3. Phân công Task (Giao thêm người)
		public async Task<bool> AssignTaskAsync(TaskAssignmentCreateDto assignmentDto, string assignerId)
		{
			var task = await _taskRepository.FirstOrDefaultAsync(t => t.TaskId == assignmentDto.TaskId);
			if (task == null) throw new ArgumentException("Task không tồn tại.");

			// 1. Kiểm tra User tồn tại và có Role đúng
			var assignee = await _userRepository.FirstOrDefaultAsync(u => u.UserId == assignmentDto.AssigneeUserId);
			if (assignee == null) throw new ArgumentException("Nhân viên không tồn tại.");

			var role = await _roleRepository.FirstOrDefaultAsync(r => r.RoleId == assignee.RoleId);
			if (role == null || role.RoleName != "maintenance_staff")
			{
				throw new ArgumentException("Người được giao việc phải là nhân viên bảo trì (maintenance_staff).");
			}

			if (task.EndDate.HasValue && task.EndDate.Value < DateTime.UtcNow)
			{
				throw new ArgumentException("Nhiệm vụ này đã quá hạn (Deadline trong quá khứ). Vui lòng cập nhật lại thời gian kết thúc trước khi phân công.");
			}

			// 2. Kiểm tra xem nhân viên này ĐÃ được giao task này chưa (Tránh trùng)
			var existingAssignment = await _assignmentRepository.FirstOrDefaultAsync(a =>
				a.TaskId == assignmentDto.TaskId && a.AssigneeUserId == assignmentDto.AssigneeUserId);

			if (existingAssignment != null)
			{
				throw new ArgumentException($"Nhân viên {assignee.Name} đã được giao nhiệm vụ này rồi.");
			}

			// 3. Validate Trùng lịch (Task Overlap)
			if (task.StartDate.HasValue && task.EndDate.HasValue)
			{
				var allAssignments = await _assignmentRepository.GetAllAsync();
				var allTasks = await _taskRepository.GetAllAsync();

				var overlappingTask = allTasks.FirstOrDefault(t =>
					allAssignments.Any(a => a.TaskId == t.TaskId && a.AssigneeUserId == assignmentDto.AssigneeUserId) &&
					(t.Status == "Assigned" || t.Status == "In Progress") &&
					t.StartDate.HasValue && t.EndDate.HasValue &&
					t.StartDate < task.EndDate && t.EndDate > task.StartDate
				);

				if (overlappingTask != null)
				{
					throw new ArgumentException($"Nhân viên {assignee.Name} đang bận xử lý công việc khác trong khung giờ này.");
				}
			}

			// 4. Tạo Assignment mới
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

			// 5. Cập nhật trạng thái Task (Nếu đang New thì chuyển sang Assigned)
			if (task.Status == "New")
			{
				task.Status = "Assigned";
				task.UpdatedAt = DateTime.UtcNow;
				await _taskRepository.UpdateAsync(task);
			}

			await _taskRepository.SaveChangesAsync();
			return true;
		}

		// 4. Lấy Task của tôi
		public async Task<PagedList<TaskDto>> GetMyTasksAsync(string assigneeId, TaskQueryParameters parameters)
		{
			var myAssignments = (await _assignmentRepository.GetAllAsync())
								.Where(a => a.AssigneeUserId == assigneeId)
								.Select(a => a.TaskId)
								.Distinct();

			var allTasks = await _taskRepository.GetAllAsync();
			var users = await _userRepository.GetAllAsync();
			var allAssignments = await _assignmentRepository.GetAllAsync();

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

		// 5. Cập nhật trạng thái
		public async Task<TaskDto?> UpdateTaskStatusAsync(string taskId, TaskUpdateStatusDto updateDto)
		{
			var task = await _taskRepository.FirstOrDefaultAsync(t => t.TaskId == taskId);
			if (task == null) return null;
			task.Status = updateDto.Status;

			if (!string.IsNullOrEmpty(updateDto.Note))
			{
				task.AssigneeNote = updateDto.Note;
			}

			task.UpdatedAt = DateTime.UtcNow;

			await _taskRepository.UpdateAsync(task);
			await _taskRepository.SaveChangesAsync();

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

		// 6. Tạo Task từ Booking
		public async Task<TaskDto> CreateTaskFromBookingAsync(string bookingId, string description, DateTime startTime, DateTime endTime, string operationStaffId)
		{
			var newTask = new ApartaAPI.Models.Task
			{
				TaskId = Guid.NewGuid().ToString(),
				ServiceBookingId = bookingId,
				OperationStaffId = operationStaffId,
				Type = "ServiceRequest",
				Description = description,
				Status = "New",
				StartDate = startTime,
				EndDate = endTime,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow,
				AssigneeNote = null
			};

			await _taskRepository.AddAsync(newTask);
			await _taskRepository.SaveChangesAsync();

			var creator = await _userRepository.FirstOrDefaultAsync(u => u.UserId == operationStaffId);
			return new TaskDto(
				newTask.TaskId, newTask.ServiceBookingId, newTask.OperationStaffId,
				creator?.Name ?? "Unknown", newTask.Type, newTask.Description,
				newTask.Status, newTask.StartDate, newTask.EndDate, newTask.CreatedAt,
				new List<TaskAssigneeDto>(), // List rỗng
				null,
				null
			);
		}

		// 7. Gỡ Nhân viên (Unassign)
		public async Task<bool> UnassignTaskAsync(string taskId, string assigneeUserId)
		{
			var task = await _taskRepository.FirstOrDefaultAsync(t => t.TaskId == taskId);
			if (task == null) throw new ArgumentException("Task không tồn tại.");

			var assignment = await _assignmentRepository.FirstOrDefaultAsync(a =>
				a.TaskId == taskId && a.AssigneeUserId == assigneeUserId);

			if (assignment == null)
			{
				throw new ArgumentException("Nhân viên này chưa được giao task này.");
			}

			// Xóa phân công
			await _assignmentRepository.RemoveAsync(assignment);

			// Kiểm tra xem Task còn ai làm không?
			var allAssignments = await _assignmentRepository.GetAllAsync();
			var remainingCount = allAssignments.Count(a => a.TaskId == taskId && a.TaskAssignmentId != assignment.TaskAssignmentId);

			// Nếu không còn ai làm, chuyển trạng thái về "New"
			if (remainingCount == 0 && task.Status != "Completed" && task.Status != "Cancelled")
			{
				task.Status = "New";
				task.UpdatedAt = DateTime.UtcNow;
				await _taskRepository.UpdateAsync(task);
			}

			return await _taskRepository.SaveChangesAsync();
		}

		// Helper Mapping (ĐÃ SỬA: Lấy List Assignees)
		private TaskDto MapToDto(ApartaAPI.Models.Task task, IEnumerable<TaskAssignment> assignments, IEnumerable<User> users)
		{
			var creator = users.FirstOrDefault(u => u.UserId == task.OperationStaffId);

			// Lấy danh sách tất cả những người được giao task này
			var assigneesList = assignments
				.Where(a => a.TaskId == task.TaskId)
				.Select(a => {
					var user = users.FirstOrDefault(u => u.UserId == a.AssigneeUserId);
					return new TaskAssigneeDto(
						a.AssigneeUserId,
						user?.Name ?? "Unknown",
						user?.Phone ?? ""
					);
				})
				.ToList();

			// Lấy ngày giao gần nhất để hiển thị (cho đẹp đội hình)
			var latestDate = assignments
				.Where(a => a.TaskId == task.TaskId)
				.OrderByDescending(a => a.AssignedDate)
				.Select(a => a.AssignedDate)
				.FirstOrDefault();

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
				assigneesList, // <-- List nhân viên
				latestDate == default ? null : (DateTime?)latestDate,
				task.AssigneeNote
			);
		}
	}
}