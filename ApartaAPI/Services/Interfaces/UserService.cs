using ApartaAPI.DTOs.User;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;

namespace ApartaAPI.Services
{
	public class UserService : IUserService
	{
		private readonly IRepository<User> _userRepository;
		private readonly IRepository<Role> _roleRepository;

		public UserService(IRepository<User> userRepository, IRepository<Role> roleRepository)
		{
			_userRepository = userRepository;
			_roleRepository = roleRepository;
		}

		public async Task<IEnumerable<StaffDto>> GetMaintenanceStaffsAsync()
		{
			var role = await _roleRepository.FirstOrDefaultAsync(r => r.RoleName == "maintenance_staff");

			if (role == null)
			{
				return Enumerable.Empty<StaffDto>();
			}

			var allUsers = await _userRepository.GetAllAsync();

			var maintenanceStaffs = allUsers
				.Where(u => u.RoleId == role.RoleId)
				.Select(u => new StaffDto(
					u.UserId,
					u.Name,
					role.RoleName
				))
				.ToList();

			return maintenanceStaffs;
		}
	}
}