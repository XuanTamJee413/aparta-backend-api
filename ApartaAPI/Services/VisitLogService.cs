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
        private readonly IRepository<StaffBuildingAssignment> _assignmentRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IMapper _mapper;

        public VisitLogService(IVisitLogRepository repository, IRepository<StaffBuildingAssignment> assignmentRepo, IRepository<User> userRepo, IMapper mapper)
        {
            _repository = repository;
            _userRepo = userRepo;
            _mapper = mapper;
            _assignmentRepo = assignmentRepo;
        }
        // get all nhung da joij bang visitor va apartment
        public async Task<PagedList<VisitLogStaffViewDto>> GetStaffViewLogsAsync(VisitorQueryParameters queryParams, string userId)
        {
            var query = _repository.GetStaffViewLogsQuery();

            // check xem staff này được gán cho những building nào và lấy danh sách tòa nhà Staff đang quản lý
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var assignedBuildingIds = await _assignmentRepo.FindAsync(x =>
                x.UserId == userId &&
                x.IsActive == true &&
                (x.AssignmentEndDate == null || x.AssignmentEndDate >= today)
            );
            var allowedBuildingIds = assignedBuildingIds.Select(x => x.BuildingId).ToList();

            // STAFF BẮT BUỘC PHẢI CÓ QUYỀN QUẢN LÝ MỚI XEM ĐƯỢC
            if (!allowedBuildingIds.Any())
            {
                return new PagedList<VisitLogStaffViewDto>(new List<VisitLogStaffViewDto>(), 0, queryParams.PageNumber, queryParams.PageSize);
            }

            // hỉ xem được tòa nhà mình quản lý
            query = query.Where(v => allowedBuildingIds.Contains(v.Apartment.BuildingId));

            //  nếu chọn cụ thể 1 tòa trong combobox
            if (!string.IsNullOrEmpty(queryParams.BuildingId))
            {
                if (!allowedBuildingIds.Contains(queryParams.BuildingId))
                {
                    return new PagedList<VisitLogStaffViewDto>(new List<VisitLogStaffViewDto>(), 0, queryParams.PageNumber, queryParams.PageSize);
                }
                query = query.Where(v => v.Apartment.BuildingId == queryParams.BuildingId);
            }

            // search/Sort/Paging
            return await ApplyCommonQueryLogic(query, queryParams);
        }

        // ==========================================================
        // Chỉ lấy theo Apartment của Resident
        // ==========================================================
        public async Task<PagedList<VisitLogStaffViewDto>> GetResidentHistoryAsync(VisitorQueryParameters queryParams, string userId)
        {
            // Lấy thông tin User để tìm ApartmentId
            var user = await _userRepo.GetByIdAsync(userId);

            // Nếu không tìm thấy user hoặc user chưa được gán vào căn hộ nào -> Rỗng
            if (user == null || string.IsNullOrEmpty(user.ApartmentId))
            {
                return new PagedList<VisitLogStaffViewDto>(new List<VisitLogStaffViewDto>(), 0, queryParams.PageNumber, queryParams.PageSize);
            }

            var query = _repository.GetStaffViewLogsQuery();

            // RESIDENT BẮT BUỘC CHỈ XEM ĐƯỢC CĂN HỘ CỦA MÌNH
            query = query.Where(v => v.ApartmentId == user.ApartmentId);

            // search/Sort/Paging
            return await ApplyCommonQueryLogic(query, queryParams);
        }

        // ==========================================================
        // HELPER: Hàm chung xử lý Search, Sort, Paging
        // ==========================================================
        private async Task<PagedList<VisitLogStaffViewDto>> ApplyCommonQueryLogic(IQueryable<VisitLog> query, VisitorQueryParameters queryParams)
        {
            // Filter theo ApartmentId (Optional cho Staff, nhưng Resident đã bị filter cứng ở trên rồi nên dòng này ko ảnh hưởng)
            if (!string.IsNullOrEmpty(queryParams.ApartmentId))
            {
                query = query.Where(v => v.ApartmentId == queryParams.ApartmentId);
            }

            // Search Term
            if (!string.IsNullOrEmpty(queryParams.SearchTerm))
            {
                var searchTermLower = queryParams.SearchTerm.ToLower();
                query = query.Where(v =>
                    (v.Visitor.FullName != null && v.Visitor.FullName.ToLower().Contains(searchTermLower)) ||
                    (v.Apartment.Code != null && v.Apartment.Code.ToLower().Contains(searchTermLower))
                );
            }

            // Sorting
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
                query = isDescending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
            }
            else
            {
                query = query.OrderByDescending(vl => vl.CheckinTime);
            }

            // Paging
            var dtoQuery = query.ProjectTo<VisitLogStaffViewDto>(_mapper.ConfigurationProvider);
            var totalCount = await dtoQuery.CountAsync();
            var items = await dtoQuery
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            return new PagedList<VisitLogStaffViewDto>(items, totalCount, queryParams.PageNumber, queryParams.PageSize);
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