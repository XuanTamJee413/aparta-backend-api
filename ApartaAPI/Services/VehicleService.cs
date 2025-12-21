using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Vehicles;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using System.Linq.Expressions;

namespace ApartaAPI.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly IRepository<Vehicle> _repository;
        private readonly IMapper _mapper;

        public VehicleService(IRepository<Vehicle> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<ApiResponse<IEnumerable<VehicleDto>>> GetAllAsync(VehicleQueryParameters query)
        {
            if (query == null)
            {
                query = new VehicleQueryParameters(null, null, null, null,null);
            }

            var searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm)
                ? null
                : query.SearchTerm.Trim().ToLowerInvariant();

            var statusFilter = string.IsNullOrWhiteSpace(query.Status)
                ? null
                : query.Status.Trim().ToLowerInvariant();

            var infoFilter = string.IsNullOrWhiteSpace(query.Info)
                ? null
                : query.Info.Trim().ToLowerInvariant();

            Expression<Func<Vehicle, bool>> predicate = v =>
                (statusFilter == null || (v.Status != null && v.Status.ToLower() == statusFilter)) &&
                (searchTerm == null ||
                    (v.VehicleNumber.ToLower().Contains(searchTerm)) ||
                    (v.ApartmentId.ToLower().Contains(searchTerm)) ||
                    (v.Info != null && v.Info.ToLower().Contains(searchTerm))
                ) &&
                (infoFilter == null || (v.Info != null && v.Info.ToLower().Contains(infoFilter))); 

            var entities = await _repository.FindAsync(predicate);

            if (entities == null)
            {
                entities = new List<Vehicle>();
            }

            var validEntities = entities.Where(e => e != null).ToList();

            IOrderedEnumerable<Vehicle> sortedEntities;
            bool isDescending = query.SortOrder?.ToLowerInvariant() == "desc";

            switch (query.SortBy?.ToLowerInvariant())
            {
                case "vehiclenumber":
                    sortedEntities = isDescending
                        ? validEntities.OrderByDescending(v => v.VehicleNumber)
                        : validEntities.OrderBy(v => v.VehicleNumber);
                    break;

                case "createdat": 
                    sortedEntities = isDescending
                        ? validEntities.OrderByDescending(v => v.CreatedAt)
                        : validEntities.OrderBy(v => v.CreatedAt);
                    break;

                default:
                    sortedEntities = validEntities.OrderByDescending(v => v.CreatedAt); 
                    break;
            }

            var dtos = _mapper.Map<IEnumerable<VehicleDto>>(sortedEntities);

            if (!dtos.Any())
            {
                return ApiResponse<IEnumerable<VehicleDto>>
                    .SuccessWithCode(new List<VehicleDto>(), ApiResponse.SM01_NO_RESULTS);
            }

            return ApiResponse<IEnumerable<VehicleDto>>.Success(dtos);
        }

        public async Task<ApiResponse<IEnumerable<VehicleDto>>> GetByUserBuildingsAsync(string userId, VehicleQueryParameters query)
        {
            if (query == null)
            {
                query = new VehicleQueryParameters(null, null, null, null,null);
            }

            var searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm)
                ? null
                : query.SearchTerm.Trim().ToLowerInvariant();

            var statusFilter = string.IsNullOrWhiteSpace(query.Status)
                ? null
                : query.Status.Trim().ToLowerInvariant();

            var infoFilter = string.IsNullOrWhiteSpace(query.Info)
                ? null
                : query.Info.Trim().ToLowerInvariant();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            Expression<Func<Vehicle, bool>> predicate = v =>
                v.Apartment != null &&
                v.Apartment.Building.StaffBuildingAssignments.Any(sba =>
                    sba.UserId == userId &&
                    sba.IsActive &&
                    (sba.AssignmentEndDate == null || sba.AssignmentEndDate >= today)
                )
                && (statusFilter == null || (v.Status != null && v.Status.ToLower() == statusFilter))
                && (searchTerm == null ||
                    (v.VehicleNumber != null && v.VehicleNumber.ToLower().Contains(searchTerm)) ||
                    (v.ApartmentId != null && v.ApartmentId.ToLower().Contains(searchTerm)) ||
                    (v.Info != null && v.Info.ToLower().Contains(searchTerm))
                )
                && (infoFilter == null || (v.Info != null && v.Info.ToLower().Contains(infoFilter))); 

            var entities = await _repository.FindAsync(predicate);

            if (entities == null)
            {
                entities = new List<Vehicle>();
            }

            var validEntities = entities.Where(e => e != null).ToList();

            IOrderedEnumerable<Vehicle> sortedEntities;
            bool isDescending = query.SortOrder?.ToLowerInvariant() == "desc";

            switch (query.SortBy?.ToLowerInvariant())
            {
                case "vehiclenumber":
                    sortedEntities = isDescending
                        ? validEntities.OrderByDescending(v => v.VehicleNumber)
                        : validEntities.OrderBy(v => v.VehicleNumber);
                    break;

                case "createdat": 
                    sortedEntities = isDescending
                        ? validEntities.OrderByDescending(v => v.CreatedAt)
                        : validEntities.OrderBy(v => v.CreatedAt);
                    break;

                default:
                    sortedEntities = validEntities.OrderByDescending(v => v.CreatedAt); 
                    break;
            }

            var dtos = _mapper.Map<IEnumerable<VehicleDto>>(sortedEntities);

            if (!dtos.Any())
            {
                return ApiResponse<IEnumerable<VehicleDto>>
                    .SuccessWithCode(new List<VehicleDto>(), ApiResponse.SM01_NO_RESULTS);
            }

            return ApiResponse<IEnumerable<VehicleDto>>.Success(dtos);
        }


        public async Task<VehicleDto?> GetByIdAsync(string id)
        {
            var entity = await _repository.FirstOrDefaultAsync(v => v.VehicleId == id);
            return _mapper.Map<VehicleDto?>(entity);
        }

        public async Task<VehicleDto> CreateAsync(VehicleCreateDto dto)
        {
            var now = DateTime.UtcNow;
            var entity = _mapper.Map<Vehicle>(dto);

            var existingVehicleNumber = await _repository.FirstOrDefaultAsync(
                a => a.VehicleNumber == entity.VehicleNumber);

            if (existingVehicleNumber != null)
            {

                var error = ApiResponse.Fail(ApiResponse.SM16_DUPLICATE_CODE, "biển số xe");
                throw new InvalidOperationException(error.Message);
            }

            entity.VehicleId = Guid.NewGuid().ToString("N");
            entity.CreatedAt = now;
            entity.UpdatedAt = now;
            entity.Status = "Chờ duyệt";

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<VehicleDto>(entity);
        }

        public async Task<bool> UpdateAsync(string id, VehicleUpdateDto dto)
        {
            var entity = await _repository.FirstOrDefaultAsync(v => v.VehicleId == id);
            if (entity == null) return false;

            _mapper.Map(dto, entity);
            entity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            return await _repository.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _repository.FirstOrDefaultAsync(v => v.VehicleId == id);
            if (entity == null) return false;

            await _repository.RemoveAsync(entity);
            return await _repository.SaveChangesAsync();
        }
    }
}
