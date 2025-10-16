using ApartaAPI.DTOs.Projects;
using ApartaAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApartaAPI.Services.Interfaces
{
    public interface IProjectService
    {
        Task<IEnumerable<ProjectDto>> GetAllAsync();
        Task<ProjectDto?> GetByIdAsync(string id);
        Task<ProjectDto> CreateAsync(ProjectCreateDto dto);
        Task<bool> UpdateAsync(string id, ProjectUpdateDto dto);
        Task<bool> DeleteAsync(string id);
    }
}