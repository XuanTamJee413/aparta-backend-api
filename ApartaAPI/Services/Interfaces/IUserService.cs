using ApartaAPI.DTOs.User;
using ApartaAPI.Models;

namespace ApartaAPI.Services.Interfaces
{
	public interface IUserService
	{
		Task<IEnumerable<StaffDto>> GetMaintenanceStaffsAsync();
		Task<User?> GetUserByApartmentIdAsync(string apartmentId);
	}
}
