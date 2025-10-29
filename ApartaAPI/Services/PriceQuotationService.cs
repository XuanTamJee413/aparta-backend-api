using ApartaAPI.DTOs.PriceQuotations;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;

namespace ApartaAPI.Services
{
    public class PriceQuotationService : IPriceQuotationService
    {
        private readonly IPriceQuotationRepository _priceQuotationRepo;
        private readonly IRepository<Building> _buildingRepo;
        private readonly IMapper _mapper;

        public PriceQuotationService(IPriceQuotationRepository priceQuotationRepo, IRepository<Building> buildingRepo, IMapper mapper)
        {
            _priceQuotationRepo = priceQuotationRepo;
            _buildingRepo = buildingRepo;
            _mapper = mapper;
        }

        // get all nhung da join voi bang building lay building code
        public async Task<IEnumerable<PriceQuotationDto>> GetPriceQuotationsAsync()
        {
            var priceQuotations = await _priceQuotationRepo.GetAllWithBuildingAsync();
            return _mapper.Map<IEnumerable<PriceQuotationDto>>(priceQuotations);
        }

        // tao moi 1 price quotation truyen building id chu 0 tryen building code, vi tim building code kho tim building hon
        // sau do check fee type co trung k0 de de~ tim theo fee type thay vi tim theo id 
        public async Task<PriceQuotationDto?> CreatePriceQuotationAsync(PriceQuotationCreateDto createDto)
        {
            var building = await _buildingRepo.FirstOrDefaultAsync(b => b.BuildingId == createDto.BuildingId);
            if (building == null)
                return null;

            var existed = await _priceQuotationRepo.FirstOrDefaultAsync(pq =>
                pq.BuildingId == createDto.BuildingId &&
                pq.FeeType == createDto.FeeType);

            if (existed != null)
                throw new InvalidOperationException($"Fee type '{createDto.FeeType}' đã tồn tại trong tòa nhà này.");

            var newPriceQuotation = _mapper.Map<PriceQuotation>(createDto);
            newPriceQuotation.PriceQuotationId = Guid.NewGuid().ToString();
            newPriceQuotation.CreatedAt = DateTime.UtcNow;
            newPriceQuotation.UpdatedAt = DateTime.UtcNow;

            await _priceQuotationRepo.AddAsync(newPriceQuotation);
            await _priceQuotationRepo.SaveChangesAsync();

            var resultDto = _mapper.Map<PriceQuotationDto>(newPriceQuotation);
            resultDto.BuildingCode = building.BuildingCode;

            return resultDto;
        }

        public async Task<IEnumerable<PriceQuotationDto>?> GetPriceQuotationsByBuildingIdAsync(string buildingId)
        {
            var building = await _buildingRepo.FirstOrDefaultAsync(b => b.BuildingId == buildingId);
            if (building == null)
            {
                return null;
            }

            var priceQuotations = await _priceQuotationRepo.GetByBuildingIdWithBuildingAsync(buildingId);

            return _mapper.Map<IEnumerable<PriceQuotationDto>>(priceQuotations);
        }

        public async Task<PriceQuotationDto?> GetPriceQuotationByIdAsync(string priceQuotationId)
        {
            var priceQuotation = await _priceQuotationRepo.GetByIdWithBuildingAsync(priceQuotationId);

            if (priceQuotation == null)
            {
                return null;
            }

            return _mapper.Map<PriceQuotationDto>(priceQuotation);
        }
    }
}
