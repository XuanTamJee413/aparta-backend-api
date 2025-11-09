using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using ApartaAPI.DTOs.Services;
using ApartaAPI.DTOs.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

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

			var allServices = await _serviceRepository.GetAllAsync();

			var query = allServices.AsQueryable();

			if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
			{
				var searchTerm = parameters.SearchTerm.Trim().ToLower();
				query = query.Where(s => s.Name.ToLower().Contains(searchTerm));
			}

			if (!string.IsNullOrWhiteSpace(parameters.Status))
			{
				var status = parameters.Status.Trim();
				query = query.Where(s => s.Status == status);
			}

			var totalCount = query.Count();

			var pagedQuery = query
				.Skip((parameters.PageNumber - 1) * parameters.PageSize)
				.Take(parameters.PageSize);

			var items = pagedQuery
				.Select(s => MapToDto(s))
				.ToList();

			return new PagedList<ServiceDto>(items, totalCount, parameters.PageNumber, parameters.PageSize);
		}


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
			var service = await _serviceRepository.FirstOrDefaultAsync(s => s.ServiceId == id);
			return service != null ? MapToDto(service) : null;
		}

		public async Task<ServiceDto> AddServiceAsync(ServiceCreateDto serviceDto)
		{
			if (serviceDto.Price.HasValue && serviceDto.Price.Value <= 0)
			{
				throw new ArgumentException("Giá dịch vụ phải lớn hơn 0.");
			}

			if (!string.IsNullOrWhiteSpace(serviceDto.Name))
			{
				var existingService = await _serviceRepository.FirstOrDefaultAsync(s => s.Name.ToLower() == serviceDto.Name.Trim().ToLower());
				if (existingService != null)
				{
					throw new InvalidOperationException($"Dịch vụ có tên '{serviceDto.Name}' đã tồn tại.");
				}
			}
			


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

			if (serviceDto.Price.HasValue && serviceDto.Price.Value <= 0)
			{
				throw new ArgumentException("Giá dịch vụ phải lớn hơn 0.");
			}

			if (!string.IsNullOrWhiteSpace(serviceDto.Name) &&
				!existingService.Name.Equals(serviceDto.Name.Trim(), StringComparison.OrdinalIgnoreCase))
			{
				var duplicateService = await _serviceRepository.FirstOrDefaultAsync(s => s.Name.ToLower() == serviceDto.Name.Trim().ToLower());
				if (duplicateService != null)
				{
					throw new InvalidOperationException($"Dịch vụ có tên '{serviceDto.Name}' đã tồn tại.");
				}
			}

			existingService.Name = serviceDto.Name ?? existingService.Name;
			existingService.Price = serviceDto.Price ?? existingService.Price;
			if (serviceDto.Status != null)
			{
				existingService.Status = serviceDto.Status;
			}
			existingService.UpdatedAt = DateTime.UtcNow;

			await _serviceRepository.UpdateAsync(existingService);
			await _serviceRepository.SaveChangesAsync();

			return MapToDto(existingService);
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