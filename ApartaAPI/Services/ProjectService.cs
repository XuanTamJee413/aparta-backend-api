using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using ApartaAPI.DTOs.Projects;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;

namespace ApartaAPI.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IRepository<Project> _repository;
        private readonly IMapper _mapper;

        public ProjectService(IRepository<Project> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProjectDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<ProjectDto>>(entities);
        }

        public async Task<ProjectDto?> GetByIdAsync(string id)
        {
            var entity = await _repository.FirstOrDefaultAsync(p => p.ProjectId == id);
            return _mapper.Map<ProjectDto?>(entity);
        }

        public async Task<ProjectDto> CreateAsync(ProjectCreateDto dto)
        {
            var now = DateTime.UtcNow;

            var entity = _mapper.Map<Project>(dto);

            if (string.IsNullOrWhiteSpace(entity.ProjectId))
                entity.ProjectId = Guid.NewGuid().ToString("N");

            entity.CreatedAt ??= now;
            entity.UpdatedAt = now;

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<ProjectDto>(entity);
        }

        public async Task<bool> UpdateAsync(string id, ProjectUpdateDto dto)
        {
            var entity = await _repository.FirstOrDefaultAsync(p => p.ProjectId == id);
            if (entity == null) return false;

            _mapper.Map(dto, entity);
            entity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            return await _repository.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _repository.FirstOrDefaultAsync(p => p.ProjectId == id);
            if (entity == null) return false;

            await _repository.RemoveAsync(entity);
            return await _repository.SaveChangesAsync();
        }
    }
}