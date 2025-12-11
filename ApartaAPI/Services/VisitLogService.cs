using ApartaAPI.Data;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.VisitLogs;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
        public async Task<PagedList<VisitLogStaffViewDto>> GetStaffViewLogsAsync(VisitorQueryParameters queryParams)
        {
            var query = _repository.GetStaffViewLogsQuery();

            if (!string.IsNullOrEmpty(queryParams.ApartmentId))
            {
                query = query.Where(v => v.ApartmentId == queryParams.ApartmentId);
            }

            if (!string.IsNullOrEmpty(queryParams.SearchTerm))
            {
                var searchTermLower = queryParams.SearchTerm.ToLower();
                query = query.Where(v =>
                    (v.Visitor.FullName != null && v.Visitor.FullName.ToLower().Contains(searchTermLower)) ||
                    (v.Apartment.Code != null && v.Apartment.Code.ToLower().Contains(searchTermLower))
                );
            }

            if (!string.IsNullOrEmpty(queryParams.SortColumn))
            {
                Expression<Func<VisitLog, object>> keySelector = queryParams.SortColumn.ToLower() switch
                {
                    "visitorfullname" => v => v.Visitor.FullName,
                    "apartmentcode" => v => v.Apartment.Code,
                    "checkintime" => v => v.CheckinTime,
                    _ => v => v.CheckinTime
                };
                bool isDescending = queryParams.SortDirection?.ToLower() == "desc";
                query = isDescending
                    ? query.OrderByDescending(keySelector)
                    : query.OrderBy(keySelector);
            }
            else
            {
                query = query.OrderByDescending(vl => vl.CheckinTime);
            }

            var dtoQuery = query.ProjectTo<VisitLogStaffViewDto>(_mapper.ConfigurationProvider);

            var totalCount = await dtoQuery.CountAsync();

            var items = await dtoQuery
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            return new PagedList<VisitLogStaffViewDto>(
                items,
                totalCount,
                queryParams.PageNumber,
                queryParams.PageSize
            );
        }
        // check-in
        public async Task<bool> CheckInAsync(string id)
        {
            var visitLog = await _repository.FirstOrDefaultAsync(v => v.VisitLogId == id);
            if (visitLog == null || visitLog.Status != "Pending")
            {
                return false; 
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
                return false; 
            }

            visitLog.Status = "Checked-out";
            visitLog.CheckoutTime = DateTime.UtcNow;

            await _repository.UpdateAsync(visitLog);
            return await _repository.SaveChangesAsync();
        }

        public async Task<bool> DeleteLogAsync(string id)
        {
            // Chỉ xóa Log, KHÔNG xóa Visitor 
            return await _repository.DeleteAsync(id);
        }

        public async Task<bool> UpdateLogAsync(string id, VisitLogUpdateDto dto)
        {
            var log = await _repository.GetByIdWithVisitorAsync(id);

            if (log == null) return false;

            //  cập nhật Visitor
            if (log.Visitor != null)
            {
                if (!string.IsNullOrEmpty(dto.FullName)) log.Visitor.FullName = dto.FullName;
                if (!string.IsNullOrEmpty(dto.Phone)) log.Visitor.Phone = dto.Phone;
                if (!string.IsNullOrEmpty(dto.IdNumber)) log.Visitor.IdNumber = dto.IdNumber;
            }

            //  cập nhật Log
            if (!string.IsNullOrEmpty(dto.Purpose)) log.Purpose = dto.Purpose;

            if (!string.IsNullOrEmpty(dto.CheckinTime) && DateTime.TryParse(dto.CheckinTime, out var newTime))
            {
                log.CheckinTime = newTime.ToUniversalTime();
            }

            await _repository.UpdateAsync(log);

            return true;
        }
    }
}