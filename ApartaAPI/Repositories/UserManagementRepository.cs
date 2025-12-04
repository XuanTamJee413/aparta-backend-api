using ApartaAPI.Data;
using ApartaAPI.DTOs.User;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.DTOs.Common; // Using PagedList from Common
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.Repositories
{
    public class UserManagementRepository : IUserManagementRepository
    {
        private readonly ApartaDbContext _context;
        public UserManagementRepository(ApartaDbContext context)
        {
            _context = context;
        }

        public async Task<PagedList<User>> GetPagedUsersAsync(UserQueryParams queryParams, List<string> rolesToInclude)
        {
            // 1. Query cơ bản
            var query = _context.Users.AsNoTracking()
                .Include(u => u.Role) // Quan trọng để map RoleName
                .Include(u => u.Apartment) // Quan trọng để map ApartmentCode cho resident
                .Where(u => !u.IsDeleted);

            // 2. Filter theo Role List
            if (rolesToInclude != null && rolesToInclude.Any())
            {
                // Chuyển role input về lower để so sánh chính xác
                var lowerRoles = rolesToInclude.Select(r => r.ToLower()).ToList();
                query = query.Where(u => u.Role != null && lowerRoles.Contains(u.Role.RoleName.ToLower()));
            }

            // 3. Filter params
            if (!string.IsNullOrEmpty(queryParams.Status))
            {
                var s = queryParams.Status.Trim().ToLower();
                query = query.Where(u => u.Status.ToLower() == s);
            }

            if (!string.IsNullOrEmpty(queryParams.SearchTerm))
            {
                var search = queryParams.SearchTerm.Trim().ToLower();
                query = query.Where(u =>
                    u.Name.ToLower().Contains(search) ||
                    (u.Email != null && u.Email.ToLower().Contains(search)) ||
                    (u.Phone != null && u.Phone.Contains(search)) ||
                    (u.StaffCode != null && u.StaffCode.ToLower().Contains(search)) ||
                    (u.Apartment != null && u.Apartment.Code.ToLower().Contains(search)) // Tìm cả theo mã căn hộ
                );
            }

            // 4. Sorting
            if (!string.IsNullOrEmpty(queryParams.SortColumn))
            {
                bool isDesc = queryParams.SortDirection?.ToLower() == "desc";
                switch (queryParams.SortColumn.ToLower())
                {
                    case "name": query = isDesc ? query.OrderByDescending(u => u.Name) : query.OrderBy(u => u.Name); break;
                    case "email": query = isDesc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email); break;
                    default: query = query.OrderByDescending(u => u.CreatedAt); break;
                }
            }
            else
            {
                query = query.OrderByDescending(u => u.CreatedAt);
            }

            // 5. Paging
            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            return new PagedList<User>(items, totalCount, queryParams.PageNumber, queryParams.PageSize);
        }

        public async Task<List<StaffBuildingAssignment>> GetStaffAssignmentsAsync(string userId)
        {
            return await _context.StaffBuildingAssignments
                .AsNoTracking()
                .Where(sba => sba.UserId == userId && sba.IsActive) // Chỉ lấy assignment đang Active
                .Include(sba => sba.Building)
                .ToListAsync();
        }
    }
}