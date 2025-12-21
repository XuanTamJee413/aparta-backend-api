using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.PriceQuotations;

namespace ApartaAPI.Services.Interfaces
{
    public interface IPriceQuotationService
    {
        // get all nhung da join voi bang building lay building code
        Task<IEnumerable<PriceQuotationDto>> GetPriceQuotationsAsync();
        // tao moi mot price quotation voi DTO khong co id va updated at vi cai nay se duoc tao tu dong
        Task<PriceQuotationDto?> CreatePriceQuotationAsync(PriceQuotationCreateDto createDto);

        Task<IEnumerable<PriceQuotationDto>?> GetPriceQuotationsByBuildingIdAsync(string buildingId);

        Task<PriceQuotationDto?> GetPriceQuotationByIdAsync(string priceQuotationId);

        Task<PagedList<PriceQuotationDto>> GetPriceQuotationsPaginatedAsync(PriceQuotationQueryParameters queryParams, string userId, bool isAdmin = false);
        Task<bool> UpdateAsync(string id, PriceQuotationCreateDto updateDto);
        Task<bool> DeleteAsync(string id);
    }
}
