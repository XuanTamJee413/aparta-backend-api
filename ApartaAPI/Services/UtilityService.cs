using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using ApartaAPI.DTOs.Utilities;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using ApartaAPI.DTOs.Common;
using Microsoft.EntityFrameworkCore; // Cần cái này để dùng .Include hoặc các hàm Async mở rộng

namespace ApartaAPI.Services
{
	public class UtilityService : IUtilityService
	{
		private readonly IRepository<Utility> _utilityRepository;
		private readonly IRepository<StaffBuildingAssignment> _assignmentRepo; // <--- Thêm Repo này
		private readonly IRepository<User> _userRepo;
		private readonly IRepository<Apartment> _apartmentRepo;

		public UtilityService(
			IRepository<Utility> utilityRepository,
			IRepository<StaffBuildingAssignment> assignmentRepo,
			IRepository<User> userRepo, 
			IRepository<Apartment> apartmentRepo)    
		{
			_utilityRepository = utilityRepository;
			_assignmentRepo = assignmentRepo;
			_userRepo = userRepo;
			_apartmentRepo = apartmentRepo;
		}

		// --- MAPPING HELPER ---
		private UtilityDto MapToDto(Utility utility) => new UtilityDto(
			utility.UtilityId,
			utility.Name,
			utility.Status,
			utility.Location,
			utility.PeriodTime,
			utility.OpenTime,
			utility.CloseTime,
			utility.BuildingId, // Map thêm BuildingId
			utility.CreatedAt,
			utility.UpdatedAt
		);

		private Utility MapToModel(UtilityCreateDto dto) => new Utility
		{
			UtilityId = Guid.NewGuid().ToString(),
			Name = dto.Name ?? string.Empty,
			Status = dto.Status ?? "Available",
			Location = dto.Location,
			PeriodTime = dto.PeriodTime,
			OpenTime = dto.OpenTime,
			CloseTime = dto.CloseTime,
			BuildingId = dto.BuildingId, // Map thêm BuildingId
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};

		// --- LOGIC CHECK QUYỀN (Helper) ---
		private async Task<List<string>> GetAllowedBuildingIdsAsync(string userId)
		{
			var today = DateOnly.FromDateTime(DateTime.UtcNow);
			var assignments = await _assignmentRepo.FindAsync(x =>
				x.UserId == userId &&
				x.IsActive == true &&
				(x.AssignmentEndDate == null || x.AssignmentEndDate >= today)
			);
			return assignments.Select(x => x.BuildingId).ToList();
		}

		// --- MAIN FUNCTIONS ---

		public async Task<PagedList<UtilityDto>> GetUtilitiesForResidentAsync(ServiceQueryParameters parameters, string residentId)
		{
			// 1. Tìm thông tin User (Cư dân)
			// Logic Mới: User -> ApartmentId -> Apartment -> BuildingId
			var user = await _userRepo.GetByIdAsync(residentId);

			// Kiểm tra nếu User không tồn tại hoặc chưa được gán vào căn hộ nào
			if (user == null || string.IsNullOrEmpty(user.ApartmentId))
			{
				return new PagedList<UtilityDto>(new List<UtilityDto>(), 0, parameters.PageNumber, parameters.PageSize);
			}

			// 2. Lấy thông tin Apartment để biết BuildingId
			var apartment = await _apartmentRepo.GetByIdAsync(user.ApartmentId);

			if (apartment == null)
			{
				// Trường hợp ID có nhưng record căn hộ không tìm thấy
				return new PagedList<UtilityDto>(new List<UtilityDto>(), 0, parameters.PageNumber, parameters.PageSize);
			}

			string residentBuildingId = apartment.BuildingId;

			// 3. Lấy danh sách tiện ích (Logic cũ giữ nguyên)
			var utilities = await _utilityRepository.GetAllAsync();
			var query = utilities.AsQueryable();

			// 4. Filter: Chỉ lấy tiện ích thuộc tòa nhà của cư dân HOẶC tiện ích chung (null)
			query = query.Where(u => u.Status == "Available" &&
									(u.BuildingId == residentBuildingId || u.BuildingId == null));

			// 5. Các bộ lọc tìm kiếm khác
			if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
			{
				var searchTerm = parameters.SearchTerm.Trim().ToLower();
				query = query.Where(u => u.Name.ToLower().Contains(searchTerm));
			}

			// 6. Phân trang & Return
			var totalCount = query.Count();
			var items = query
				.OrderBy(u => u.Name)
				.Skip((parameters.PageNumber - 1) * parameters.PageSize)
				.Take(parameters.PageSize)
				.Select(u => MapToDto(u))
				.ToList();

			return new PagedList<UtilityDto>(items, totalCount, parameters.PageNumber, parameters.PageSize);
		}
		public async Task<PagedList<UtilityDto>> GetAllUtilitiesAsync(ServiceQueryParameters parameters, string userId)
		{
			// 1. Lấy danh sách tòa nhà Staff được quản lý
			var allowedBuildingIds = await GetAllowedBuildingIdsAsync(userId);

			if (!allowedBuildingIds.Any())
			{
				// Nếu không quản lý tòa nào -> Trả về rỗng
				return new PagedList<UtilityDto>(new List<UtilityDto>(), 0, parameters.PageNumber, parameters.PageSize);
			}

			var utilities = await _utilityRepository.GetAllAsync();
			var query = utilities.AsQueryable();

			// 2. Filter: Chỉ lấy Utility thuộc các tòa nhà được phép
			// (Hoặc u.BuildingId == null nếu là tiện ích chung cho cả khu đô thị)
			query = query.Where(u => u.BuildingId != null && allowedBuildingIds.Contains(u.BuildingId));

			// --- Logic Search/Filter cũ ---
			if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
			{
				var searchTerm = parameters.SearchTerm.Trim().ToLower();
				query = query.Where(u => u.Name.ToLower().Contains(searchTerm));
			}

			if (!string.IsNullOrWhiteSpace(parameters.Status))
			{
				var status = parameters.Status.Trim();
				query = query.Where(u => u.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
			}

			var totalCount = query.Count();
			var items = query
				.Skip((parameters.PageNumber - 1) * parameters.PageSize)
				.Take(parameters.PageSize)
				.Select(u => MapToDto(u))
				.ToList();

			return new PagedList<UtilityDto>(items, totalCount, parameters.PageNumber, parameters.PageSize);
		}

		public async Task<UtilityDto?> GetUtilityByIdAsync(string id, string userId)
		{
			var utility = await _utilityRepository.FirstOrDefaultAsync(u => u.UtilityId == id);
			if (utility == null) return null;

			// Check quyền: Nếu Staff không quản lý tòa nhà của tiện ích này -> Trả về null (hoặc throw Forbidden)
			var allowedBuildingIds = await GetAllowedBuildingIdsAsync(userId);
			if (utility.BuildingId != null && !allowedBuildingIds.Contains(utility.BuildingId))
			{
				return null; // Coi như không tìm thấy vì không có quyền
			}

			return MapToDto(utility);
		}

		public async Task<UtilityDto> AddUtilityAsync(UtilityCreateDto utilityDto, string userId)
		{
			// 1. Validate BuildingId
			if (string.IsNullOrEmpty(utilityDto.BuildingId))
			{
				throw new ArgumentException("Phải chỉ định tòa nhà cho tiện ích.");
			}

			// 2. Check quyền: Staff có được quản lý tòa nhà này không?
			var allowedBuildingIds = await GetAllowedBuildingIdsAsync(userId);
			if (!allowedBuildingIds.Contains(utilityDto.BuildingId))
			{
				throw new UnauthorizedAccessException("Bạn không có quyền thêm tiện ích cho tòa nhà này.");
			}

			// --- Logic Validate cũ ---
			if (utilityDto.PeriodTime.HasValue && utilityDto.PeriodTime.Value < 1)
				throw new ArgumentException("Thời gian sử dụng (PeriodTime) phải lớn hơn 0.");

			if (utilityDto.OpenTime.HasValue && utilityDto.CloseTime.HasValue && utilityDto.OpenTime >= utilityDto.CloseTime)
				throw new ArgumentException("Giờ mở cửa phải nhỏ hơn giờ đóng cửa.");

			if (string.IsNullOrWhiteSpace(utilityDto.Name))
				throw new ArgumentException("Tên tiện ích không được để trống.");

			// Check trùng tên (Trong phạm vi tòa nhà hoặc toàn cục tùy nghiệp vụ)
			// Ở đây check trùng tên trong cùng 1 tòa nhà thì hợp lý hơn
			var trimmedName = utilityDto.Name.Trim();
			var existingByName = await _utilityRepository.FirstOrDefaultAsync(u =>
				u.Name.ToLower() == trimmedName.ToLower() &&
				u.BuildingId == utilityDto.BuildingId); // Check trùng trong cùng tòa

			if (existingByName != null)
				throw new InvalidOperationException($"Tiện ích '{trimmedName}' đã tồn tại trong tòa nhà này.");

			var newUtility = MapToModel(utilityDto);
			var addedUtility = await _utilityRepository.AddAsync(newUtility);
			await _utilityRepository.SaveChangesAsync();

			return MapToDto(addedUtility);
		}

		public async Task<UtilityDto?> UpdateUtilityAsync(string id, UtilityUpdateDto utilityDto, string userId)
		{
			var existingUtility = await _utilityRepository.FirstOrDefaultAsync(u => u.UtilityId == id);
			if (existingUtility == null) return null;

			// 1. Check quyền: Staff có quản lý tòa nhà của tiện ích này không?
			var allowedBuildingIds = await GetAllowedBuildingIdsAsync(userId);
			if (existingUtility.BuildingId != null && !allowedBuildingIds.Contains(existingUtility.BuildingId))
			{
				throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa tiện ích của tòa nhà này.");
			}

			// --- Logic Validate cũ ---
			if (utilityDto.PeriodTime.HasValue && utilityDto.PeriodTime.Value < 1)
				throw new ArgumentException("Thời gian sử dụng (PeriodTime) phải lớn hơn 0.");

			var newOpenTime = utilityDto.OpenTime ?? existingUtility.OpenTime;
			var newCloseTime = utilityDto.CloseTime ?? existingUtility.CloseTime;

			if (newOpenTime.HasValue && newCloseTime.HasValue && newOpenTime >= newCloseTime)
				throw new ArgumentException("Giờ mở cửa phải nhỏ hơn giờ đóng cửa.");

			if (!string.IsNullOrWhiteSpace(utilityDto.Name))
			{
				var trimmedName = utilityDto.Name.Trim();
				if (!existingUtility.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase))
				{
					// Check trùng tên trong cùng tòa nhà
					var existingByName = await _utilityRepository.FirstOrDefaultAsync(u =>
						u.Name.ToLower() == trimmedName.ToLower() &&
						u.BuildingId == existingUtility.BuildingId && // Cùng tòa
						u.UtilityId != id);

					if (existingByName != null)
						throw new InvalidOperationException($"Tiện ích '{trimmedName}' đã tồn tại.");
				}
			}

			// Update fields
			existingUtility.Name = utilityDto.Name ?? existingUtility.Name;
			existingUtility.Status = utilityDto.Status ?? existingUtility.Status;
			existingUtility.Location = utilityDto.Location ?? existingUtility.Location;
			existingUtility.PeriodTime = utilityDto.PeriodTime ?? existingUtility.PeriodTime;
			existingUtility.OpenTime = utilityDto.OpenTime ?? existingUtility.OpenTime;
			existingUtility.CloseTime = utilityDto.CloseTime ?? existingUtility.CloseTime;
			existingUtility.UpdatedAt = DateTime.UtcNow;

			await _utilityRepository.UpdateAsync(existingUtility);
			await _utilityRepository.SaveChangesAsync();

			return MapToDto(existingUtility);
		}

		public async Task<bool> DeleteUtilityAsync(string id, string userId)
		{
			var utilityToDelete = await _utilityRepository.FirstOrDefaultAsync(u => u.UtilityId == id);
			if (utilityToDelete == null) return false;

			// 1. Check quyền
			var allowedBuildingIds = await GetAllowedBuildingIdsAsync(userId);
			if (utilityToDelete.BuildingId != null && !allowedBuildingIds.Contains(utilityToDelete.BuildingId))
			{
				throw new UnauthorizedAccessException("Bạn không có quyền xóa tiện ích của tòa nhà này.");
			}

			await _utilityRepository.RemoveAsync(utilityToDelete);
			return await _utilityRepository.SaveChangesAsync();
		}
	}
}