using ApartaAPI.Data;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace ApartaAPI.Repositories
{
    public class UserManagementRepository : Repository<User>, IUserManagementRepository
    {
        public UserManagementRepository(ApartaDbContext context) : base(context)
        {
        }

        public IQueryable<User> GetUsersQuery(List<string> rolesToInclude)
        {
            // Sử dụng _dbSet từ lớp cha (Repository<T>)
            // Deferred Execution: Chỉ xây dựng câu lệnh, chưa query DB
            var query = _dbSet.AsNoTracking()
                .Where(u => !u.IsDeleted);

            // Filter theo Role List
            if (rolesToInclude != null && rolesToInclude.Any())
            {
                // Chuyển về chữ thường để so sánh chính xác
                var lowerRoles = rolesToInclude.Select(r => r.ToLower()).ToList();

                // EF Core sẽ tự động Join bảng Role khi truy cập u.Role
                query = query.Where(u => u.Role != null && lowerRoles.Contains(u.Role.RoleName));
            }

            return query;
        }

        public IQueryable<StaffBuildingAssignment> GetStaffAssignmentsQuery(string userId)
        {
            // Sử dụng _context (protected từ lớp cha) để truy cập DbSet khác
            return _context.StaffBuildingAssignments
                .AsNoTracking()
                .Where(sba => sba.UserId == userId && sba.IsActive)
                .Include(sba => sba.Building);
        }
    }
}