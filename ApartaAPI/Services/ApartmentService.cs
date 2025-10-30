using ApartaAPI.DTOs.Apartments;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using System.Linq.Expressions;

namespace ApartaAPI.Services
{
    public class ApartmentService : IApartmentService
    {
        private readonly IRepository<Apartment> _repository;
        private readonly IMapper _mapper;

        public ApartmentService(IRepository<Apartment> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<ApiResponse<IEnumerable<ApartmentDto>>> GetAllAsync(ApartmentQueryParameters query)
        {
            if (query == null)
            {
                query = new ApartmentQueryParameters(null, null, null, null, null);
            }

            var searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm)
                ? null
                : query.SearchTerm.Trim().ToLowerInvariant();

            var statusFilter = string.IsNullOrWhiteSpace(query.Status)
                ? null
                : query.Status.Trim().ToLowerInvariant();

            var buildingIdFilter = string.IsNullOrWhiteSpace(query.BuildingId)
                ? null
                : query.BuildingId.Trim();

            Expression<Func<Apartment, bool>> predicate = a =>
                (buildingIdFilter == null || a.BuildingId == buildingIdFilter) &&
                (statusFilter == null || (a.Status != null && a.Status.ToLower() == statusFilter)) &&
                (searchTerm == null ||
                    (a.Code.ToLower().Contains(searchTerm)) || 
                    (a.Type != null && a.Type.ToLower().Contains(searchTerm))
                );

            var entities = await _repository.FindAsync(predicate);

            if (entities == null)
            {
                entities = new List<Apartment>();
            }

            var validEntities = entities.Where(e => e != null).ToList();

            IOrderedEnumerable<Apartment> sortedEntities;
            bool isDescending = query.SortOrder?.ToLowerInvariant() == "desc";

            switch (query.SortBy?.ToLowerInvariant())
            {
                case "code":
                    sortedEntities = isDescending
                        ? validEntities.OrderByDescending(a => a.Code)
                        : validEntities.OrderBy(a => a.Code);
                    break;
                case "area":
                    sortedEntities = isDescending
                        ? validEntities.OrderByDescending(a => a.Area)
                        : validEntities.OrderBy(a => a.Area);
                    break;
                default:
                    sortedEntities = validEntities.OrderByDescending(a => a.CreatedAt);
                    break;
            }

            var dtos = _mapper.Map<IEnumerable<ApartmentDto>>(sortedEntities);

            if (!dtos.Any())
            {
                return ApiResponse<IEnumerable<ApartmentDto>>.Success(new List<ApartmentDto>(), "SM01"); 
            }

            return ApiResponse<IEnumerable<ApartmentDto>>.Success(dtos);
        }

        public async Task<ApartmentDto?> GetByIdAsync(string id)
        {
            var entity = await _repository.FirstOrDefaultAsync(a => a.ApartmentId == id);
            return _mapper.Map<ApartmentDto?>(entity);
        }

        public async Task<ApartmentDto> CreateAsync(ApartmentCreateDto dto)
        {
            var now = DateTime.UtcNow;
            var entity = _mapper.Map<Apartment>(dto);

            entity.ApartmentId = Guid.NewGuid().ToString("N");
            entity.CreatedAt = now;
            entity.UpdatedAt = now;

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<ApartmentDto>(entity);
        }

        public async Task<bool> UpdateAsync(string id, ApartmentUpdateDto dto)
        {
            var entity = await _repository.FirstOrDefaultAsync(a => a.ApartmentId == id);
            if (entity == null) return false;

            _mapper.Map(dto, entity);
            entity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            return await _repository.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(string id)
        {
           
            var entity = await _repository.FirstOrDefaultAsync(a => a.ApartmentId == id);
            if (entity == null) return false;

            try
            {
                await _repository.RemoveAsync(entity);
                return await _repository.SaveChangesAsync();
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}