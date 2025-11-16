using ApartaAPI.DTOs.User;

namespace ApartaAPI.Services.Interfaces
{
	public interface IUserService
	{
		Task<IEnumerable<StaffDto>> GetMaintenanceStaffsAsync();
	}
}
