using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using ApartaAPI.DTOs.Utilities;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic; // Cần thiết cho ICollection

namespace ApartaAPI.Services
{
	public class UtilityService : IUtilityService
	{
		private readonly IRepository<Utility> _utilityRepository;

		public UtilityService(IRepository<Utility> utilityRepository)
		{
			_utilityRepository = utilityRepository;
		}

		// ĐÃ SỬA: Cập nhật MapToDto để bao gồm Location và PeriodTime
		private UtilityDto MapToDto(Utility utility) => new UtilityDto(
			utility.UtilityId,
			utility.Name,
			utility.Status,
			utility.Location,    // Bao gồm Location
			utility.PeriodTime,  // Bao gồm PeriodTime
			utility.CreatedAt,
			utility.UpdatedAt
		);

		// ĐÃ SỬA: Cập nhật MapToModel để bao gồm Location và PeriodTime
		private Utility MapToModel(UtilityCreateDto dto) => new Utility
		{
			UtilityId = Guid.NewGuid().ToString(),
			Name = dto.Name ?? string.Empty, // Xử lý null
			Status = dto.Status ?? "Available", // Gán giá trị mặc định nếu cần
			Location = dto.Location,
			PeriodTime = dto.PeriodTime,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};


		public async Task<IEnumerable<UtilityDto>> GetAllUtilitiesAsync()
		{
			var utilities = await _utilityRepository.GetAllAsync();
			return utilities.Select(u => MapToDto(u)).ToList();
		}

		public async Task<UtilityDto?> GetUtilityByIdAsync(string id)
		{
			var utility = await _utilityRepository.FirstOrDefaultAsync(u => u.UtilityId == id);
			return utility != null ? MapToDto(utility) : null;
		}

		public async Task<UtilityDto> AddUtilityAsync(UtilityCreateDto utilityDto)
		{
			var newUtility = MapToModel(utilityDto);

			var addedUtility = await _utilityRepository.AddAsync(newUtility);
			await _utilityRepository.SaveChangesAsync();

			return MapToDto(addedUtility);
		}

		// ĐÃ SỬA: Cập nhật UpdateUtilityAsync để xử lý Location và PeriodTime
		public async Task<UtilityDto?> UpdateUtilityAsync(string id, UtilityUpdateDto utilityDto)
		{
			var existingUtility = await _utilityRepository.FirstOrDefaultAsync(u => u.UtilityId == id);

			if (existingUtility == null)
			{
				return null;
			}

			// Cập nhật các trường, chỉ thay đổi nếu giá trị mới được cung cấp (không null)
			existingUtility.Name = utilityDto.Name ?? existingUtility.Name;
			existingUtility.Status = utilityDto.Status ?? existingUtility.Status;

			// Cập nhật trường mới
			existingUtility.Location = utilityDto.Location ?? existingUtility.Location;
			existingUtility.PeriodTime = utilityDto.PeriodTime ?? existingUtility.PeriodTime;

			existingUtility.UpdatedAt = DateTime.UtcNow; // Update timestamp

			var updatedUtility = await _utilityRepository.UpdateAsync(existingUtility);
			await _utilityRepository.SaveChangesAsync();

			return MapToDto(updatedUtility);
		}

		public async Task<bool> DeleteUtilityAsync(string id)
		{
			var utilityToDelete = await _utilityRepository.FirstOrDefaultAsync(u => u.UtilityId == id);

			if (utilityToDelete == null)
			{
				return false;
			}

			await _utilityRepository.RemoveAsync(utilityToDelete);
			return await _utilityRepository.SaveChangesAsync();
		}
	}
}