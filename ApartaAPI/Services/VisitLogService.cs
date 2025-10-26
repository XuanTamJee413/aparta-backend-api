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

        public VisitLogService(IVisitLogRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }
        // get all nhung da joij bang visitor va apartment
        public async Task<IEnumerable<VisitLogStaffViewDto>> GetStaffViewLogsAsync()
        {
            var entities = await _repository.GetStaffViewLogsAsync();
            return _mapper.Map<IEnumerable<VisitLogStaffViewDto>>(entities);
        }
        // check-in
        public async Task<bool> CheckInAsync(string id)
        {
            var visitLog = await _repository.FirstOrDefaultAsync(v => v.VisitLogId == id);
            if (visitLog == null || visitLog.Status != "Pending")
            {
                return false; // Thất bại
            }

            visitLog.Status = "Checked-in";
            visitLog.CheckinTime = DateTime.UtcNow;

            await _repository.UpdateAsync(visitLog);
            return await _repository.SaveChangesAsync();
        }
        // check-out
        public async Task<bool> CheckOutAsync(string id)
        {
            var visitLog = await _repository.FirstOrDefaultAsync(v => v.VisitLogId == id);
            if (visitLog == null || visitLog.Status != "Checked-in")
            {
                return false; // Thất bại
            }

            visitLog.Status = "Checked-out";
            visitLog.CheckoutTime = DateTime.UtcNow;

            await _repository.UpdateAsync(visitLog);
            return await _repository.SaveChangesAsync();
        }
    }
}