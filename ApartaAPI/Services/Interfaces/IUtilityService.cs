using ApartaAPI.DTOs.Utilities; 
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApartaAPI.Services.Interfaces
{
	public interface IUtilityService
	{
		Task<IEnumerable<UtilityDto>> GetAllUtilitiesAsync();
		Task<UtilityDto?> GetUtilityByIdAsync(string id);
		Task<UtilityDto> AddUtilityAsync(UtilityCreateDto utilityDto);
		Task<UtilityDto?> UpdateUtilityAsync(string id, UtilityUpdateDto utilityDto);
		Task<bool> DeleteUtilityAsync(string id);
	}
}