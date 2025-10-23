using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Projects;
using ApartaAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApartaAPI.Services.Interfaces
{
    public interface IProjectService
    {
        Task<ApiResponse<IEnumerable<ProjectDto>>> GetAllAsync(ProjectQueryParameters query);
        Task<ApiResponse<ProjectDto>> GetByIdAsync(string id);
        Task<ApiResponse<ProjectDto>> CreateAsync(ProjectCreateDto dto);
        Task<ApiResponse> UpdateAsync(string id, ProjectUpdateDto dto);
    }
}