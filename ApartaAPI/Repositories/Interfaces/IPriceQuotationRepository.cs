using ApartaAPI.Models;

namespace ApartaAPI.Repositories.Interfaces
{
    public interface IPriceQuotationRepository : IRepository<PriceQuotation>
    {
        Task<IEnumerable<PriceQuotation>> GetAllWithBuildingAsync();

        Task<IEnumerable<PriceQuotation>> GetByBuildingIdWithBuildingAsync(string buildingId);

        Task<PriceQuotation?> GetByIdWithBuildingAsync(string priceQuotationId);

        IQueryable<PriceQuotation> GetQuotationsQueryable();
    }
}
