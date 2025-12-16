using ApartaAPI.Models;

namespace ApartaAPI.Repositories.Interfaces
{
    public interface IVisitorRepository : IRepository<Visitor>
    {
        Task<IEnumerable<Visitor>> GetRecentVisitorsByApartmentAsync(string apartmentId);
    }
}
