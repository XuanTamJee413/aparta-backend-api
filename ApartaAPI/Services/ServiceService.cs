using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using ApartaAPI.DTOs.Services;
using ApartaAPI.DTOs.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
// Microsoft.EntityFrameworkCore không còn cần thiết cho hàm này nữa
// vì chúng ta không dùng .CountAsync() hay .ToListAsync()

namespace ApartaAPI.Services
{
	public class ServiceService : IServiceService
	{
		private readonly IRepository<Service> _serviceRepository;

		public ServiceService(IRepository<Service> serviceRepository)
		{
			_serviceRepository = serviceRepository;
		}

		public async Task<PagedList<ServiceDto>> GetServicesAsync(ServiceQueryParameters parameters)
		{
			// 1. TẢI TẤT CẢ DỮ LIỆU TỪ DB VỀ BỘ NHỚ TRƯỚC TIÊN! 
			// Đây là điểm khác biệt lớn nhất (và là điểm yếu về hiệu năng).
			var allServices = await _serviceRepository.GetAllAsync();

			// 2. Chuyển sang IQueryable (in-memory) để xây dựng truy vấn
			var query = allServices.AsQueryable();

			// 3. Áp dụng TÌM KIẾM (Search) - TRONG BỘ NHỚ
			if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
			{
				var searchTerm = parameters.SearchTerm.Trim().ToLower();
				query = query.Where(s => s.Name.ToLower().Contains(searchTerm));
			}

			// 4. Áp dụng LỌC (Filter) - TRONG BỘ NHỚ
			if (!string.IsNullOrWhiteSpace(parameters.Status))
			{
				var status = parameters.Status.Trim();
				query = query.Where(s => s.Status == status);
			}

			// 5. Lấy tổng số lượng (TRƯỚC KHI Phân Trang) - TRONG BỘ NHỚ
			// Bỏ 'await' và '.CountAsync()', dùng '.Count()' đồng bộ
			var totalCount = query.Count();

			// 6. Áp dụng PHÂN TRANG (Pagination) - TRONG BỘ NHỚ
			var pagedQuery = query
				.Skip((parameters.PageNumber - 1) * parameters.PageSize)
				.Take(parameters.PageSize);

			// 7. Thực thi truy vấn và Map sang DTO - TRONG BỘ NHỚ
			// Bỏ 'await' và '.ToListAsync()', dùng '.ToList()' đồng bộ
			var items = pagedQuery
				.Select(s => MapToDto(s))
				.ToList();

			// 8. Trả về đối tượng PagedList
			return new PagedList<ServiceDto>(items, totalCount, parameters.PageNumber, parameters.PageSize);
		}

		// ... (Các hàm MapToDto, AddServiceAsync, v.v... giữ nguyên) ...

		private ServiceDto MapToDto(Service service) => new ServiceDto(
			service.ServiceId,
			service.Name,
			service.Price,
			service.Status,
			service.CreatedAt,
			service.UpdatedAt
		);

		private Service MapToModel(ServiceCreateDto dto) => new Service
		{
			ServiceId = Guid.NewGuid().ToString(),
			Name = dto.Name ?? string.Empty,
			Price = dto.Price ?? 0m,
			Status = dto.Status ?? "Available",
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};


		public async Task<ServiceDto?> GetServiceByIdAsync(string id)
		{
			// Giả sử hàm này đã được tối ưu (ví dụ: dùng FirstOrDefaultAsync)
			var service = await _serviceRepository.FirstOrDefaultAsync(s => s.ServiceId == id);
			return service != null ? MapToDto(service) : null;
		}

		public async Task<ServiceDto> AddServiceAsync(ServiceCreateDto serviceDto)
		{
			var newService = MapToModel(serviceDto);

			var addedService = await _serviceRepository.AddAsync(newService);
			await _serviceRepository.SaveChangesAsync();

			return MapToDto(addedService);
		}

		public async Task<ServiceDto?> UpdateServiceAsync(string id, ServiceUpdateDto serviceDto)
		{
			var existingService = await _serviceRepository.FirstOrDefaultAsync(s => s.ServiceId == id);

			if (existingService == null)
			{
				return null;
			}

			existingService.Name = serviceDto.Name ?? existingService.Name;
			existingService.Price = serviceDto.Price ?? existingService.Price;
			if (serviceDto.Status != null)
			{
				existingService.Status = serviceDto.Status;
			}
			existingService.UpdatedAt = DateTime.UtcNow;

			var updatedService = await _serviceRepository.UpdateAsync(existingService);
			await _serviceRepository.SaveChangesAsync();

			return MapToDto(updatedService);
		}

		public async Task<bool> DeleteServiceAsync(string id)
		{
			var serviceToDelete = await _serviceRepository.FirstOrDefaultAsync(s => s.ServiceId == id);

			if (serviceToDelete == null)
			{
				return false;
			}

			await _serviceRepository.RemoveAsync(serviceToDelete);
			return await _serviceRepository.SaveChangesAsync();
		}
	}
}