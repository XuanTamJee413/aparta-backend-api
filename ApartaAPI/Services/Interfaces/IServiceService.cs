using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Services; 
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApartaAPI.Services.Interfaces
{
	public interface IServiceService
	{
		Task<PagedList<ServiceDto>> GetServicesAsync(ServiceQueryParameters parameters);

		Task<ServiceDto?> GetServiceByIdAsync(string id);
		Task<ServiceDto> AddServiceAsync(ServiceCreateDto serviceDto);
		Task<ServiceDto?> UpdateServiceAsync(string id, ServiceUpdateDto serviceDto);
		Task<bool> DeleteServiceAsync(string id);
	}
}