using ApartaAPI.Data;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.PriceQuotations;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.Services
{
    public class PriceQuotationService : IPriceQuotationService
    {
        private readonly IPriceQuotationRepository _priceQuotationRepo;
        private readonly IRepository<Building> _buildingRepo;
        private readonly IMapper _mapper;
        private readonly ApartaDbContext _context;

        public PriceQuotationService(IPriceQuotationRepository priceQuotationRepo, IRepository<Building> buildingRepo,
            IMapper mapper, ApartaDbContext context
)
        {
            _priceQuotationRepo = priceQuotationRepo;
            _buildingRepo = buildingRepo;
            _mapper = mapper;
            _context = context;
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

        public async Task<bool> UpdateAsync(string id, PriceQuotationCreateDto updateDto)
        {
            var entity = await _priceQuotationRepo.FirstOrDefaultAsync(pq => pq.PriceQuotationId == id);
            if (entity == null)
                return false;

            if (entity.FeeType != updateDto.FeeType)
            {
                var existed = await _priceQuotationRepo.FirstOrDefaultAsync(pq =>
                    pq.BuildingId == entity.BuildingId &&
                    pq.FeeType == updateDto.FeeType &&
                    pq.PriceQuotationId != id);

                if (existed != null)
                {
                    throw new InvalidOperationException($"Fee type '{updateDto.FeeType}' đã tồn tại trong tòa nhà này.");
                }
            }

            _mapper.Map(updateDto, entity);
            entity.UpdatedAt = DateTime.UtcNow;

            await _priceQuotationRepo.UpdateAsync(entity);
            return await _priceQuotationRepo.SaveChangesAsync();
        }
        public async Task<PagedList<PriceQuotationDto>> GetPriceQuotationsPaginatedAsync(PriceQuotationQueryParameters queryParams, string managerId)
        {
            // 1. Lấy danh sách ID các tòa nhà mà Manager này quản lý từ bảng StaffBuildingAssignment
            // Lưu ý: Cần inject thêm DbContext hoặc BuildingAssignment Repository nếu chưa có
            var managedBuildingIds = await _context.StaffBuildingAssignments
                .Where(sba => sba.UserId == managerId && sba.IsActive)
                .Select(sba => sba.BuildingId)
                .ToListAsync();

            // 2. Lấy Query gốc
            var query = _priceQuotationRepo.GetQuotationsQueryable();

            // 3. LỌC CỨNG: Chỉ lấy Price Quotation thuộc các tòa nhà Manager quản lý
            query = query.Where(q => managedBuildingIds.Contains(q.BuildingId));

            // 4. Nếu trên UI người dùng chọn lọc cụ thể 1 tòa nhà (trong số những tòa họ quản lý)
            if (!string.IsNullOrEmpty(queryParams.BuildingId))
            {
                query = query.Where(q => q.BuildingId == queryParams.BuildingId);
            }

            // 5. Các logic Search và Sort giữ nguyên
            if (!string.IsNullOrEmpty(queryParams.SearchTerm))
            {
                string searchTermLower = queryParams.SearchTerm.ToLower();
                query = query.Where(q => q.FeeType.ToLower().Contains(searchTermLower));
            }

            // Sorting...
            query = queryParams.SortColumn?.ToLower() == "feetype"
                ? (queryParams.SortDirection?.ToLower() == "asc" ? query.OrderBy(q => q.FeeType) : query.OrderByDescending(q => q.FeeType))
                : query.OrderByDescending(q => q.CreatedAt);

            // Projection và Paging
            var dtoQuery = query.ProjectTo<PriceQuotationDto>(_mapper.ConfigurationProvider);
            var totalCount = await dtoQuery.CountAsync();
            var items = await dtoQuery
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            return new PagedList<PriceQuotationDto>(items, totalCount, queryParams.PageNumber, queryParams.PageSize);
        }
        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _priceQuotationRepo.FirstOrDefaultAsync(pq => pq.PriceQuotationId == id);
            if (entity == null || entity.IsDeleted == true)
                return false;

            // xóa
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;

            await _priceQuotationRepo.UpdateAsync(entity);

            bool success = true;
            //success = await _priceQuotationRepo.SaveChangesAsync();
            return success;
        }
    }
}
