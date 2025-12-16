using ApartaAPI.Models;
using System.Collections.Generic;
using System.Linq;

namespace ApartaAPI.Repositories.Interfaces
{
    public interface IUserManagementRepository : IRepository<User>
    {
        /// <summary>
        /// Trả về IQueryable để Service thực hiện Filter/Sort/Project/Paging.
        /// Áp dụng các điều kiện cơ bản: Chưa xóa và thuộc danh sách Role cho trước.
        /// </summary>
        IQueryable<User> GetUsersQuery(List<string> rolesToInclude);

        /// <summary>
        /// Trả về IQueryable cho Assignments (nếu cần query riêng lẻ, dù ProjectTo đã hỗ trợ).
        /// </summary>
        IQueryable<StaffBuildingAssignment> GetStaffAssignmentsQuery(string userId);
    }
}