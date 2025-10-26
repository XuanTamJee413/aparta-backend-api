using ApartaAPI.Data;
using ApartaAPI.DTOs.VisitLogs;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.Services
{
    public class VisitLogService : IVisitLogService
    {
        private readonly IVisitLogRepository _repository;
        private readonly IMapper _mapper;
        ApartaDbContext _context;

        public VisitLogService(IVisitLogRepository repository, IMapper mapper, ApartaDbContext context)
        {
            _repository = repository;
            _mapper = mapper;
            _context = context;
        }
        // get all tho^
        public async Task<IEnumerable<VisitLogDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<VisitLogDto>>(entities);
        }
        // get all nhung da joij bang visitor va apartment
        public async Task<IEnumerable<VisitLogStaffViewDto>> GetStaffViewLogsAsync()
        {
            var entities = await _repository.GetStaffViewLogsAsync();
            return _mapper.Map<IEnumerable<VisitLogStaffViewDto>>(entities);
        }
        public async Task<VisitLogDto?> GetByIdAsync(string id)
        {
            var entity = await _repository.FirstOrDefaultAsync(v => v.VisitLogId == id);
            return _mapper.Map<VisitLogDto?>(entity);
        }

        // tamnx: create 1 visitlog only
        public async Task<VisitLogDto> CreateAsync(VisitLogCreateDto dto)
        {
            var entity = _mapper.Map<VisitLog>(dto);

            entity.VisitLogId = Guid.NewGuid().ToString("N");
            entity.CheckinTime = DateTime.Now;
            entity.Status ??= "CheckedIn"; 

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<VisitLogDto>(entity);
        }

        public async Task<bool> UpdateAsync(string id, VisitLogUpdateDto dto)
        {
            var entity = await _repository.FirstOrDefaultAsync(v => v.VisitLogId == id);
            if (entity == null) return false;

            _mapper.Map(dto, entity);

            await _repository.UpdateAsync(entity);
            return await _repository.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _repository.FirstOrDefaultAsync(v => v.VisitLogId == id);
            if (entity == null) return false;

            await _repository.RemoveAsync(entity);
            return await _repository.SaveChangesAsync();
        }

        public async Task<IEnumerable<VisitLogHistoryDto>> GetByApartmentIdAsync(string apartmentId)
        {
            var logs = await _context.VisitLogs
                .Include(vl => vl.Visitor)
                .Where(vl => vl.ApartmentId == apartmentId)
                .ToListAsync();

            return _mapper.Map<IEnumerable<VisitLogHistoryDto>>(logs);
        }

        public async Task<VisitLogDto?> CheckInAsync(string id)
        {
            var visitLog = await _repository.FirstOrDefaultAsync(v => v.VisitLogId == id);
            if (visitLog == null || visitLog.Status != "Pending")
            {
                return null;
            }

            visitLog.Status = "Checked-in";
            visitLog.CheckinTime = DateTime.UtcNow;

            await _repository.UpdateAsync(visitLog);
            await _repository.SaveChangesAsync();

            return _mapper.Map<VisitLogDto>(visitLog);
        }

        public async Task<VisitLogDto?> CheckOutAsync(string id)
        {
            var visitLog = await _repository.FirstOrDefaultAsync(v => v.VisitLogId == id);
            if (visitLog == null || visitLog.Status != "Checked-in")
            {
                return null;
            }

            visitLog.Status = "Checked-out";
            visitLog.CheckoutTime = DateTime.UtcNow;

            await _repository.UpdateAsync(visitLog);
            await _repository.SaveChangesAsync();

            return _mapper.Map<VisitLogDto>(visitLog);
        }
    }
}