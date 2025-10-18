using AutoMapper;
using ApartaAPI.DTOs.VisitLogs;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;

namespace ApartaAPI.Services
{
    public class VisitLogService : IVisitLogService
    {
        private readonly IRepository<VisitLog> _repository;
        private readonly IMapper _mapper;

        public VisitLogService(IRepository<VisitLog> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<VisitLogDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<VisitLogDto>>(entities);
        }

        public async Task<VisitLogDto?> GetByIdAsync(string id)
        {
            var entity = await _repository.FirstOrDefaultAsync(v => v.Id == id);
            return _mapper.Map<VisitLogDto?>(entity);
        }

        public async Task<VisitLogDto> CreateAsync(VisitLogCreateDto dto)
        {
            var entity = _mapper.Map<VisitLog>(dto);

            entity.Id = Guid.NewGuid().ToString("N");
            entity.CheckinTime = DateTime.UtcNow;
            entity.Status ??= "CheckedIn"; 

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<VisitLogDto>(entity);
        }

        public async Task<bool> UpdateAsync(string id, VisitLogUpdateDto dto)
        {
            var entity = await _repository.FirstOrDefaultAsync(v => v.Id == id);
            if (entity == null) return false;

            _mapper.Map(dto, entity);

            await _repository.UpdateAsync(entity);
            return await _repository.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _repository.FirstOrDefaultAsync(v => v.Id == id);
            if (entity == null) return false;

            await _repository.RemoveAsync(entity);
            return await _repository.SaveChangesAsync();
        }
    }
}