
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using ApartaAPI.DTOs.Services; // Ensure correct namespace for DTOs
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

		private ServiceDto MapToDto(Service service) => new ServiceDto(
			service.ServiceId,
			service.Name,
			service.Price,
			service.CreatedAt,
			service.UpdatedAt
		);

		private Service MapToModel(ServiceCreateDto dto) => new Service
		{
			ServiceId = dto.ServiceId ?? Guid.NewGuid().ToString(), 
			Name = dto.Name,
			Price = dto.Price,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};


		public async Task<IEnumerable<ServiceDto>> GetAllServicesAsync()
		{
			var services = await _serviceRepository.GetAllAsync();
			return services.Select(s => MapToDto(s)).ToList();
		}

		public async Task<ServiceDto?> GetServiceByIdAsync(string id)
		{
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

			existingService.Name = serviceDto.Name;
			existingService.Price = serviceDto.Price;
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