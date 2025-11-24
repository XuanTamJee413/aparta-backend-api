using ApartaAPI.Models;

namespace ApartaAPI.Repositories.Interfaces
{
    public interface IStaffAssignmentRepository : IRepository<StaffBuildingAssignment>
    {
        Task<StaffBuildingAssignment?> GetActiveAssignmentAsync(string userId, string buildingId);

        IQueryable<StaffBuildingAssignment> GetAssignmentsQuery();
    }
}