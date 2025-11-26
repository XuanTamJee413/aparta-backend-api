using ApartaAPI.DTOs.Apartments;
using ApartaAPI.DTOs.Buildings;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ApartaAPI.Services
{
    public class BuildingService : IBuildingService
    {
        private readonly IRepository<Building> _repository;
        private readonly IRepository<Project> _projectRepository;
        private readonly IApartmentService _apartmentService;
        private readonly IMapper _mapper;

        public BuildingService(
            IRepository<Building> repository,
            IRepository<Project> projectRepository,
            IApartmentService apartmentService,
            IMapper mapper)
        {
            _repository = repository;
            _projectRepository = projectRepository;
            _apartmentService = apartmentService;
            _mapper = mapper;
        }

        // (UC 2.1.6) - Modified for Pagination and Search only
        public async Task<ApiResponse<PaginatedResult<BuildingDto>>> GetAllAsync(BuildingQueryParameters query)
        {
            try
            {
                var searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm)
                    ? null
                    : query.SearchTerm.Trim().ToLowerInvariant();

                // Build predicate for database filtering (Search only)
                Expression<Func<Building, bool>> predicate = b =>
                    (searchTerm == null ||
                        (b.Name.ToLower().Contains(searchTerm) ||
                         b.BuildingCode.ToLower().Contains(searchTerm)));

                var allEntities = await _repository.FindAsync(predicate);

                // Apply pagination in memory
                var totalCount = allEntities.Count();
                var paginatedEntities = allEntities
                                        .OrderBy(b => b.BuildingCode)
                                        .Skip(query.Skip)
                                        .Take(query.Take)
                                        .ToList();

                var dtos = _mapper.Map<IEnumerable<BuildingDto>>(paginatedEntities);
                var paginatedResult = new PaginatedResult<BuildingDto>(dtos, totalCount);

                if (totalCount == 0)
                {
                    return ApiResponse<PaginatedResult<BuildingDto>>.Success(paginatedResult, ApiResponse.SM01_NO_RESULTS);
                }

                return ApiResponse<PaginatedResult<BuildingDto>>.Success(paginatedResult);
            }
            catch (Exception)
            {
                return ApiResponse<PaginatedResult<BuildingDto>>.Fail(ApiResponse.SM15_PAYMENT_FAILED);
            }
        }

        // Standard GetById
        public async Task<ApiResponse<BuildingDto>> GetByIdAsync(string id)
        {
            try
            {
                var entity = await _repository.FirstOrDefaultAsync(b => b.BuildingId == id);

                if (entity == null)
                {
                    return ApiResponse<BuildingDto>.Fail(ApiResponse.SM01_NO_RESULTS);
                }

                var dto = _mapper.Map<BuildingDto>(entity);
                return ApiResponse<BuildingDto>.Success(dto);
            }
            catch (Exception)
            {
                return ApiResponse<BuildingDto>.Fail(ApiResponse.SM15_PAYMENT_FAILED);
            }
        }

        // (UC 2.1.7)
        public async Task<ApiResponse<BuildingDto>> CreateAsync(BuildingCreateDto dto)
        {
            try
            {
                // Validation: Required fields
                if (string.IsNullOrWhiteSpace(dto.ProjectId))
                {
                    return ApiResponse<BuildingDto>.Fail(ApiResponse.SM02_REQUIRED);
                }

                if (string.IsNullOrWhiteSpace(dto.BuildingCode))
                {
                    return ApiResponse<BuildingDto>.Fail(ApiResponse.SM02_REQUIRED);
                }

                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return ApiResponse<BuildingDto>.Fail(ApiResponse.SM02_REQUIRED);
                }

                // Validation: Check if Project exists
                var project = await _projectRepository.FirstOrDefaultAsync(p => p.ProjectId == dto.ProjectId);
                if (project == null)
                {
                    return ApiResponse<BuildingDto>.Fail(ApiResponse.SM01_NO_RESULTS);
                }

                // Validation: Duplicate Building Code within the same Project
                var exists = await _repository.FirstOrDefaultAsync(b => 
                    b.ProjectId == dto.ProjectId && 
                    b.BuildingCode == dto.BuildingCode);
                
                if (exists != null)
                {
                    return ApiResponse<BuildingDto>.Fail(ApiResponse.SM16_DUPLICATE_CODE, "BuildingCode");
                }

                var now = DateTime.UtcNow;
                var entity = _mapper.Map<Building>(dto);

                entity.CreatedAt = now;
                entity.UpdatedAt = now;
                entity.IsActive = true;

                await _repository.AddAsync(entity);
                await _repository.SaveChangesAsync();

                var resultDto = _mapper.Map<BuildingDto>(entity);
                return ApiResponse<BuildingDto>.SuccessWithCode(resultDto, ApiResponse.SM04_CREATE_SUCCESS, "Building");
            }
            catch (Exception)
            {
                return ApiResponse<BuildingDto>.Fail(ApiResponse.SM15_PAYMENT_FAILED);
            }
        }

        // (UC 2.1.8) - Includes Deactivation
        public async Task<ApiResponse> UpdateAsync(string id, BuildingUpdateDto dto)
        {
            try
            {
                var entity = await _repository.FirstOrDefaultAsync(b => b.BuildingId == id);

                if (entity == null)
                {
                    return ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS);
                }

                // Validation: Required field Name if provided
                if (dto.Name != null && string.IsNullOrWhiteSpace(dto.Name))
                {
                    return ApiResponse.Fail(ApiResponse.SM02_REQUIRED);
                }

                // Check if there are no actual changes
                bool hasChanges = false;

                if (dto.Name != null && dto.Name != entity.Name)
                    hasChanges = true;

                if (dto.IsActive.HasValue && dto.IsActive.Value != entity.IsActive)
                    hasChanges = true;

                if (!hasChanges)
                {
                    return ApiResponse.Success(ApiResponse.SM20_NO_CHANGES);
                }

                // BR-19: Building Code cannot be changed - handled by DTO and Mapper config
                _mapper.Map(dto, entity);
                entity.UpdatedAt = DateTime.UtcNow;

                await _repository.UpdateAsync(entity);
                await _repository.SaveChangesAsync();

                return ApiResponse.Success(ApiResponse.SM03_UPDATE_SUCCESS);
            }
            catch (Exception)
            {
                return ApiResponse.Fail(ApiResponse.SM15_PAYMENT_FAILED);
            }
        }

        public async Task<ApiResponse<IEnumerable<ApartmentDto>>> GetRentedApartmentsByBuildingAsync(string buildingId)
        {
            try
            {
                // Kiểm tra Building tồn tại và is_active = true
                var building = await _repository.FirstOrDefaultAsync(b => b.BuildingId == buildingId);
                if (building == null)
                {
                    return ApiResponse<IEnumerable<ApartmentDto>>.Fail(ApiResponse.SM01_NO_RESULTS);
                }

                if (!building.IsActive)
                {
                    return ApiResponse<IEnumerable<ApartmentDto>>.Fail(ApiResponse.SM26_BUILDING_NOT_ACTIVE);
                }

                // Kiểm tra Project tồn tại và is_active = true
                var project = await _projectRepository.FirstOrDefaultAsync(p => p.ProjectId == building.ProjectId);
                if (project == null)
                {
                    return ApiResponse<IEnumerable<ApartmentDto>>.Fail(ApiResponse.SM27_PROJECT_NOT_FOUND);
                }

                if (!project.IsActive)
                {
                    return ApiResponse<IEnumerable<ApartmentDto>>.Fail(ApiResponse.SM28_PROJECT_NOT_ACTIVE);
                }

                // Lấy danh sách căn hộ có status = "Đã thuê" thuộc building này
                var query = new ApartmentQueryParameters(
                    BuildingId: buildingId,
                    Status: "Đã thuê",
                    SearchTerm: null,
                    SortBy: null,
                    SortOrder: null);
                var response = await _apartmentService.GetAllAsync(query);

                return response;
            }
            catch (Exception)
            {
                return ApiResponse<IEnumerable<ApartmentDto>>.Fail(ApiResponse.SM15_PAYMENT_FAILED);
            }
        }
    }
}