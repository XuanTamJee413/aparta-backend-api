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
        private readonly IMapper _mapper;

        public BuildingService(IRepository<Building> repository, IMapper mapper)
        {
            _repository = repository;
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
                // Removed IsActive filter as requested

                var allEntities = await _repository.FindAsync(predicate); // Fetch all matching search term

                // Apply pagination in memory
                var totalCount = allEntities.Count();
                var paginatedEntities = allEntities
                                        .OrderBy(b => b.BuildingCode) // Default order if none specified
                                        .Skip(query.Skip)
                                        .Take(query.Take)
                                        .ToList(); // Execute query here

                var dtos = _mapper.Map<IEnumerable<BuildingDto>>(paginatedEntities);
                var paginatedResult = new PaginatedResult<BuildingDto>(dtos, totalCount);

                if (totalCount == 0)
                {
                    return ApiResponse<PaginatedResult<BuildingDto>>.Success(paginatedResult, "SM01"); // SM01 for no results
                }

                return ApiResponse<PaginatedResult<BuildingDto>>.Success(paginatedResult);
            }
            catch (Exception)
            {
                return ApiResponse<PaginatedResult<BuildingDto>>.Fail("An unexpected error occurred."); // Generic error message
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
                    return ApiResponse<BuildingDto>.Fail("SM01"); // SM01 Not found
                }

                var dto = _mapper.Map<BuildingDto>(entity);
                return ApiResponse<BuildingDto>.Success(dto);
            }
            catch (Exception)
            {
                return ApiResponse<BuildingDto>.Fail("An unexpected error occurred.");
            }
        }

        // (UC 2.1.7)
        public async Task<ApiResponse<BuildingDto>> CreateAsync(BuildingCreateDto dto)
        {
            try
            {
                // Validation: Required fields
                if (string.IsNullOrWhiteSpace(dto.ProjectId) || string.IsNullOrWhiteSpace(dto.BuildingCode) || string.IsNullOrWhiteSpace(dto.Name))
                {
                    return ApiResponse<BuildingDto>.Fail("SM02"); // SM02 Required field missing
                }

                // Validation: Duplicate Building Code within the same Project
                var exists = await _repository.FirstOrDefaultAsync(b => b.ProjectId == dto.ProjectId && b.BuildingCode == dto.BuildingCode);
                if (exists != null)
                {
                    return ApiResponse<BuildingDto>.Fail("SM16"); // SM16 Duplicate code
                }

                var now = DateTime.UtcNow;
                var entity = _mapper.Map<Building>(dto);

                entity.CreatedAt = now;
                entity.UpdatedAt = now;
                entity.IsActive = true;

                await _repository.AddAsync(entity);
                await _repository.SaveChangesAsync();

                var resultDto = _mapper.Map<BuildingDto>(entity);
                // BR-10: Logging handled by interceptor/middleware ideally, but basic log here.
                return ApiResponse<BuildingDto>.Success(resultDto, "SM04"); // SM04 Create success
            }
            catch (Exception ex)
            {
                return ApiResponse<BuildingDto>.Fail("An unexpected error occurred during creation.");
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
                    return ApiResponse.Fail("SM01"); // SM01 Not found
                }

                // Validation: Required field Name if provided
                if (dto.Name != null && string.IsNullOrWhiteSpace(dto.Name))
                {
                    return ApiResponse.Fail("SM02"); // SM02 Required field missing (if name is being updated to empty)
                }

                // BR-19: Building Code cannot be changed - handled by DTO and Mapper config.
                _mapper.Map(dto, entity);
                entity.UpdatedAt = DateTime.UtcNow;

                await _repository.UpdateAsync(entity);
                await _repository.SaveChangesAsync();

                // BR-10: Logging handled by interceptor/middleware ideally, but basic log here.
                return ApiResponse.Success("SM03"); // SM03 Update success
            }
            catch (Exception)
            {
                return ApiResponse.Fail("An unexpected error occurred during update.");
            }
        }
    }
}