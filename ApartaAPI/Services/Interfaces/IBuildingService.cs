﻿using ApartaAPI.DTOs.Buildings;
using ApartaAPI.DTOs.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApartaAPI.Services.Interfaces
{
    public interface IBuildingService
    {
        Task<ApiResponse<PaginatedResult<BuildingDto>>> GetAllAsync(BuildingQueryParameters query);
        Task<ApiResponse<BuildingDto>> GetByIdAsync(string id);
        Task<ApiResponse<BuildingDto>> CreateAsync(BuildingCreateDto dto);
        Task<ApiResponse> UpdateAsync(string id, BuildingUpdateDto dto);
    }
}