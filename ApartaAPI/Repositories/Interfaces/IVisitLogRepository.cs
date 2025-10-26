using ApartaAPI.Models;

namespace ApartaAPI.Repositories.Interfaces
{
    public interface IVisitLogRepository : IRepository<VisitLog>
    {
        Task<IEnumerable<VisitLog>> GetStaffViewLogsAsync();
    }
}
