using AutoMapper;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using ApartaAPI.DTOs.Assets;
using ApartaAPI.DTOs.Common; 
using System.Linq.Expressions; 


namespace ApartaAPI.Services
{
    public class AssetService : IAssetService
    {
        private readonly IRepository<Asset> _repository;
        private readonly IMapper _mapper;

        public AssetService(IRepository<Asset> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<ApiResponse<IEnumerable<AssetDto>>> GetAllAsync(AssetQueryParameters query)
        {
            if (query == null)
            {
                query = new AssetQueryParameters(null, null, null, null);
            }

            var searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm)
                ? null
                : query.SearchTerm.Trim().ToLowerInvariant();

            var buildingIdFilter = string.IsNullOrWhiteSpace(query.BuildingId)
                ? null
                : query.BuildingId.Trim();

            Expression<Func<Asset, bool>> predicate = a =>
                (buildingIdFilter == null || a.BuildingId == buildingIdFilter) &&
                (searchTerm == null ||
                    (a.Info != null && a.Info.ToLower().Contains(searchTerm))
                );

            var entities = await _repository.FindAsync(predicate);

            if (entities == null)
            {
                entities = new List<Asset>();
            }

            var validEntities = entities.Where(e => e != null).ToList();

            IOrderedEnumerable<Asset> sortedEntities;
            bool isDescending = query.SortOrder?.ToLowerInvariant() == "desc";

            switch (query.SortBy?.ToLowerInvariant())
            {
                case "info":
                    sortedEntities = isDescending
                        ? validEntities.OrderByDescending(a => a.Info ?? string.Empty)
                        : validEntities.OrderBy(a => a.Info ?? string.Empty);
                    break;
                case "quantity":
                    sortedEntities = isDescending
                        ? validEntities.OrderByDescending(a => a.Quantity)
                        : validEntities.OrderBy(a => a.Quantity);
                    break;
                default:
                    sortedEntities = validEntities.OrderByDescending(a => a.CreatedAt);
                    break;
            }

            var dtos = _mapper.Map<IEnumerable<AssetDto>>(sortedEntities);

            if (!dtos.Any())
            {
                return ApiResponse<IEnumerable<AssetDto>>.Success(new List<AssetDto>(), "SM01"); 
            }

            return ApiResponse<IEnumerable<AssetDto>>.Success(dtos);
        }


        public async Task<AssetDto?> GetByIdAsync(string id)
        {
            var entity = await _repository.FirstOrDefaultAsync(a => a.AssetId == id);
            return _mapper.Map<AssetDto?>(entity);
        }

        public async Task<AssetDto> CreateAsync(AssetCreateDto dto)
        {
            var now = DateTime.UtcNow;
            var entity = _mapper.Map<Asset>(dto);

            entity.AssetId = Guid.NewGuid().ToString("N");
            entity.CreatedAt = now;
            entity.UpdatedAt = now;

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<AssetDto>(entity);
        }

        public async Task<bool> UpdateAsync(string id, AssetUpdateDto dto)
        {
            var entity = await _repository.FirstOrDefaultAsync(a => a.AssetId == id);
            if (entity == null) return false;

            _mapper.Map(dto, entity);
            entity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            return await _repository.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _repository.FirstOrDefaultAsync(a => a.AssetId == id);
            if (entity == null) return false;

            await _repository.RemoveAsync(entity);
            return await _repository.SaveChangesAsync();
        }
    }
}