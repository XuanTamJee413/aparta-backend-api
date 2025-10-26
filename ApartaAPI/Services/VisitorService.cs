using ApartaAPI.DTOs.Visitors;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ApartaAPI.Services
{
    public class VisitorService : IVisitorService
    {
        private readonly IRepository<Visitor> _repository;
        private readonly IMapper _mapper;

        private readonly IRepository<VisitLog> _visitLogRepository;

        public VisitorService(
            IRepository<Visitor> repository,
            IMapper mapper,
            IRepository<VisitLog> visitLogRepository) 
        {
            _repository = repository;
            _mapper = mapper;
            _visitLogRepository = visitLogRepository; 
        }

        public async Task<IEnumerable<VisitorDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<VisitorDto>>(entities);
        }

        public async Task<VisitorDto?> GetByIdAsync(string id)
        {
            var entity = await _repository.FirstOrDefaultAsync(v => v.VisitorId == id);
            return _mapper.Map<VisitorDto?>(entity);
        }

        // tamnx: create a visitor with status pending waiting for staff accepts, both create vissitor and visitlog pending
        public async Task<VisitorDto> CreateAsync(VisitorCreateDto dto)
        {
            var entity = _mapper.Map<Visitor>(dto);
            if (string.IsNullOrWhiteSpace(entity.VisitorId))
                entity.VisitorId = Guid.NewGuid().ToString("N");

            await _repository.AddAsync(entity); // add to visitor

            if (!string.IsNullOrWhiteSpace(dto.ApartmentId))
            {
                var visitLog = new VisitLog
                {
                    VisitLogId = Guid.NewGuid().ToString("N"),
                    VisitorId = entity.VisitorId,
                    ApartmentId = dto.ApartmentId,
                    Purpose = dto.Purpose,
                    CheckinTime = dto.CheckinTime ?? DateTime.Now,
                    Status = "Pending"
                };

                await _visitLogRepository.AddAsync(visitLog); // add to visit log
            }

            await _repository.SaveChangesAsync();

            return _mapper.Map<VisitorDto>(entity);
        }

        public async Task<bool> UpdateAsync(string id, VisitorUpdateDto dto)
        {
            var entity = await _repository.FirstOrDefaultAsync(v => v.VisitorId == id);
            if (entity == null) return false;

            _mapper.Map(dto, entity);

            await _repository.UpdateAsync(entity);
            return await _repository.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _repository.FirstOrDefaultAsync(v => v.VisitorId == id);
            if (entity == null) return false;

            await _repository.RemoveAsync(entity);
            return await _repository.SaveChangesAsync();
        }
    }
}
