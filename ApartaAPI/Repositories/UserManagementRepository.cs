/* --- File: Repositories/UserManagementRepository.cs --- */

using ApartaAPI.Data;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.User;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
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
            // TRUY VẤN NÂNG CAO TẠM THỜI ĐỂ TEST CASE-INSENSITIVE CHO ROLE
            var query = _context.Users
                .Where(u => !u.IsDeleted && u.Role != null && rolesToInclude.Select(r => r.ToLower()).Contains(u.Role.RoleName.ToLower()));
            // Lọc theo Status
            if (!string.IsNullOrEmpty(queryParams.Status))
            {
                query = query.Where(u => u.Status.ToLower() == queryParams.Status.ToLower());
            }

            // Tìm kiếm theo SearchTerm
            if (!string.IsNullOrEmpty(queryParams.SearchTerm))
            {
                var search = queryParams.SearchTerm.ToLower();
                query = query.Where(u => u.Name.ToLower().Contains(search) ||
                                                         u.Email!.ToLower().Contains(search) ||
                                                         u.Phone!.ToLower().Contains(search) ||

                                              (u.StaffCode != null && u.StaffCode.ToLower().Contains(search)));
            }

            // Sắp xếp (Sort) - Cần đảm bảo cột tồn tại
            if (!string.IsNullOrEmpty(queryParams.SortColumn) && !string.IsNullOrEmpty(queryParams.SortDirection))
            {
                // Logic sắp xếp phức tạp nên được xử lý thông qua method mở rộng hoặc thư viện bên ngoài.
                // Ở đây, ta chỉ sắp xếp theo CreatedAt làm mặc định
                if (queryParams.SortDirection.ToLower() == "desc")
                    query = query.OrderByDescending(u => u.CreatedAt);
                else
                    query = query.OrderBy(u => u.CreatedAt);
            }
            else
            {
                query = query.OrderByDescending(u => u.CreatedAt);
            }

            // Phân trang: THAY THẾ logic CreateAsync bằng cách thực hiện phân trang thủ công
            var source = query.Include(u => u.Role).AsNoTracking();

            // 1. Đếm tổng số lượng
            var totalCount = await source.CountAsync();

            // 2. Lấy dữ liệu phân trang
            var items = await source
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            // 3. Khởi tạo và trả về PagedList
            return new PagedList<User>(items, totalCount, queryParams.PageNumber, queryParams.PageSize);
        }

        public async Task<List<StaffBuildingAssignment>> GetStaffAssignmentsAsync(string userId)
        {
            return await _context.StaffBuildingAssignments
                .Where(sba => sba.UserId == userId)
                .Include(sba => sba.Building)
                .AsNoTracking()

    .ToListAsync();
        }
    }
}