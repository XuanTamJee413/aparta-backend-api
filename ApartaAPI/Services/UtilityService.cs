using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using ApartaAPI.DTOs.Utilities;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using ApartaAPI.DTOs.Common;

namespace ApartaAPI.Services
{
	public class UtilityService : IUtilityService
	{
		private readonly IRepository<Utility> _utilityRepository;

		public UtilityService(IRepository<Utility> utilityRepository)
		{
			_utilityRepository = utilityRepository;
		}

		private UtilityDto MapToDto(Utility utility) => new UtilityDto(
			utility.UtilityId,
			utility.Name,
			utility.Status,
			utility.Location,   
			utility.PeriodTime,  
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
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};


		public async Task<PagedList<UtilityDto>> GetAllUtilitiesAsync(ServiceQueryParameters parameters)
		{
			var utilities = await _utilityRepository.GetAllAsync();
			var query = utilities.AsQueryable();

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

			var pagedQuery = query
				.Skip((parameters.PageNumber - 1) * parameters.PageSize)
				.Take(parameters.PageSize);

			var items = pagedQuery
				.Select(u => MapToDto(u))
				.ToList();
			return new PagedList<UtilityDto>(items, totalCount, parameters.PageNumber, parameters.PageSize);
		}

		public async Task<UtilityDto?> GetUtilityByIdAsync(string id)
		{
			var utility = await _utilityRepository.FirstOrDefaultAsync(u => u.UtilityId == id);
			return utility != null ? MapToDto(utility) : null;
		}

		public async Task<UtilityDto> AddUtilityAsync(UtilityCreateDto utilityDto)
		{
			if (utilityDto.PeriodTime.HasValue && utilityDto.PeriodTime.Value < 1)
			{
				throw new ArgumentException("Thời gian sử dụng (PeriodTime) phải lớn hơn 1.");
			}

			if (string.IsNullOrWhiteSpace(utilityDto.Name))
			{
				throw new ArgumentException("Tên tiện ích không được để trống.");
			}

			var trimmedName = utilityDto.Name.Trim();
			var existingByName = await _utilityRepository.FirstOrDefaultAsync(u => u.Name.ToLower() == trimmedName.ToLower());
			if (existingByName != null)
			{
				throw new InvalidOperationException($"Tiện ích có tên '{trimmedName}' đã tồn tại.");
			}

			var newUtility = MapToModel(utilityDto);
			var addedUtility = await _utilityRepository.AddAsync(newUtility);
			await _utilityRepository.SaveChangesAsync();

			return MapToDto(addedUtility);
		}

		public async Task<UtilityDto?> UpdateUtilityAsync(string id, UtilityUpdateDto utilityDto)
		{
			var existingUtility = await _utilityRepository.FirstOrDefaultAsync(u => u.UtilityId == id);

			if (existingUtility == null)
			{
				return null;
			}

			if (utilityDto.PeriodTime.HasValue && utilityDto.PeriodTime.Value <= 1)
			{
				throw new ArgumentException("Thời gian sử dụng (PeriodTime) phải lớn hơn 1.");
			}

			if (!string.IsNullOrWhiteSpace(utilityDto.Name))
			{
				var trimmedName = utilityDto.Name.Trim();
				if (!existingUtility.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase))
				{
					var existingByName = await _utilityRepository.FirstOrDefaultAsync(u => u.Name.ToLower() == trimmedName.ToLower());
					if (existingByName != null)
					{
						throw new InvalidOperationException($"Tiện ích có tên '{trimmedName}' đã tồn tại.");
					}
				}
			}

			existingUtility.Name = utilityDto.Name ?? existingUtility.Name;
			existingUtility.Status = utilityDto.Status ?? existingUtility.Status;
			existingUtility.Location = utilityDto.Location ?? existingUtility.Location;
			existingUtility.PeriodTime = utilityDto.PeriodTime ?? existingUtility.PeriodTime;
			existingUtility.UpdatedAt = DateTime.UtcNow;

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