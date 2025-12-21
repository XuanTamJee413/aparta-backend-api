using ApartaAPI.Data;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.VisitLogs;
using ApartaAPI.DTOs.Visitors;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;

namespace ApartaAPI.Services
{
    public class VisitLogService : IVisitLogService
    {
        private readonly IVisitorRepository _visitorRepository;
        private readonly IVisitLogRepository _visitLogRepository;
        private readonly IRepository<Apartment> _apartmentRepository;
        private readonly IRepository<StaffBuildingAssignment> _assignmentRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IMapper _mapper;

        public VisitLogService(
            IVisitLogRepository visitLogRepository,
            IVisitorRepository visitorRepository,
            IRepository<Apartment> apartmentRepository,
            IRepository<StaffBuildingAssignment> assignmentRepo,
            IRepository<User> userRepo,
            IMapper mapper)
        {
            _visitorRepository = visitorRepository;
            _visitLogRepository = visitLogRepository;
            _apartmentRepository = apartmentRepository;
            _assignmentRepo = assignmentRepo;
            _userRepo = userRepo;
            _mapper = mapper;
        }

        // ==========================================================
        // 1. TẠO MỚI LỊCH HẸN (Resident đăng ký khách)
        // ==========================================================
        public async Task<VisitorDto> CreateVisitAsync(VisitorCreateDto dto)
        {
            var apartmentExists = await _apartmentRepository.FirstOrDefaultAsync(a => a.ApartmentId == dto.ApartmentId || a.Code == dto.ApartmentId);
            if (apartmentExists == null)
            {
                throw new ValidationException($"Căn hộ với ID '{dto.ApartmentId}' không tồn tại.");
            }

            var visitor = await _visitorRepository.FirstOrDefaultAsync(v => v.IdNumber == dto.IdNumber);
            bool isVisitorUpdated = false;

            if (visitor == null)
            {
                visitor = _mapper.Map<Visitor>(dto);
                visitor.VisitorId = Guid.NewGuid().ToString();
                await _visitorRepository.AddAsync(visitor);
            }
            else
            {
                visitor.FullName = dto.FullName;
                visitor.Phone = dto.Phone;
                await _visitorRepository.UpdateAsync(visitor);
                isVisitorUpdated = true;
            }

            var newVisitLog = _mapper.Map<VisitLog>(dto);
            newVisitLog.VisitLogId = Guid.NewGuid().ToString(); 
            newVisitLog.VisitorId = visitor.VisitorId;
            newVisitLog.VisitorId = visitor.VisitorId;
            newVisitLog.ApartmentId = apartmentExists.ApartmentId;
            newVisitLog.CheckoutTime = null;

            if (string.IsNullOrWhiteSpace(dto.CheckinTime))
            {
                newVisitLog.CheckinTime = DateTime.UtcNow.AddDays(1);
                newVisitLog.Status = "Pending";
            }
            else
            {
                if (DateTime.TryParse(dto.CheckinTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedTime))
                {
                    var utcTime = parsedTime.ToUniversalTime();
                    if (utcTime <= DateTime.UtcNow.AddMinutes(-5))
                    {
                        throw new ValidationException("Thời gian check-in dự kiến không thể ở trong quá khứ.");
                    }
                    newVisitLog.CheckinTime = utcTime;
                }
                else
                {
                    throw new ValidationException("Định dạng thời gian check-in không hợp lệ.");
                }
                newVisitLog.Status = dto.Status ?? "Pending";
            }

            await _visitLogRepository.AddAsync(newVisitLog);
            await _visitLogRepository.SaveChangesAsync();

            var resultDto = _mapper.Map<VisitorDto>(visitor);
            resultDto.IsUpdated = isVisitorUpdated;
            return resultDto;
        }

        // ==========================================================
        // 2. CẬP NHẬT LỊCH HẸN (Sửa thông tin - GIẢI QUYẾT DUPLICATE KEY)
        // ==========================================================
        public async Task<bool> UpdateLogAsync(string id, VisitLogUpdateDto dto)
        {
            // 1. Tìm bản ghi log kèm thông tin người khách
            // Hãy đảm bảo hàm này trong Repository có .Include(v => v.Visitor)
            var log = await _visitLogRepository.GetByIdWithVisitorAsync(id);

            if (log == null)
            {
                // Debug để biết tại sao không tìm thấy
                Console.WriteLine($"[Error] 404 Not Found: VisitLogId {id} does not exist in DB.");
                return false;
            }

            // 2. CẬP NHẬT THÔNG TIN NGƯỜI KHÁCH (Bảng Visitors)
            if (log.Visitor != null)
            {
                // Kiểm tra nếu người dùng đổi CCCD (idNumber) sang một người khác đã tồn tại
                var existingOtherVisitor = await _visitorRepository.FirstOrDefaultAsync(v =>
                    v.IdNumber == dto.IdNumber && v.VisitorId != log.VisitorId);

                if (existingOtherVisitor != null)
                {
                    // Nếu trùng CCCD người khác -> Gán lịch hẹn này cho người khách đó
                    log.VisitorId = existingOtherVisitor.VisitorId;

                    // Cập nhật thông tin mới nhất cho người khách đó
                    existingOtherVisitor.FullName = dto.FullName ?? existingOtherVisitor.FullName;
                    existingOtherVisitor.Phone = dto.Phone ?? existingOtherVisitor.Phone;
                    await _visitorRepository.UpdateAsync(existingOtherVisitor);
                }
                else
                {
                    // Nếu không trùng ai -> Cập nhật thông tin cho khách hiện tại
                    log.Visitor.FullName = dto.FullName ?? log.Visitor.FullName;
                    log.Visitor.Phone = dto.Phone ?? log.Visitor.Phone;
                    log.Visitor.IdNumber = dto.IdNumber ?? log.Visitor.IdNumber;
                    await _visitorRepository.UpdateAsync(log.Visitor);
                }
            }

            // 3. CẬP NHẬT THÔNG TIN NHẬT KÝ (Bảng VisitLogs)
            if (!string.IsNullOrEmpty(dto.Purpose)) log.Purpose = dto.Purpose;

            if (!string.IsNullOrEmpty(dto.CheckinTime) && DateTime.TryParse(dto.CheckinTime, out var newTime))
            {
                // Đảm bảo lưu đúng giờ UTC
                log.CheckinTime = newTime.ToUniversalTime();
            }

            // 4. LƯU THAY ĐỔI TOÀN CỤC
            await _visitLogRepository.UpdateAsync(log);

            // SaveChangesAsync sẽ thực thi cả 2 lệnh Update của Visitor và VisitLog trong 1 Transaction
            return await _visitLogRepository.SaveChangesAsync();
        }

        // ==========================================================
        // 3. CÁC HÀM TRUY VẤN VÀ TRẠNG THÁI
        // ==========================================================
        public async Task<PagedList<VisitLogStaffViewDto>> GetStaffViewLogsAsync(VisitorQueryParameters queryParams, string userId)
        {
            var query = _visitLogRepository.GetStaffViewLogsQuery();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var assignedBuildingIds = await _assignmentRepo.FindAsync(x =>
                x.UserId == userId && x.IsActive == true && (x.AssignmentEndDate == null || x.AssignmentEndDate >= today));

            var allowedBuildingIds = assignedBuildingIds.Select(x => x.BuildingId).ToList();
            if (!allowedBuildingIds.Any()) return new PagedList<VisitLogStaffViewDto>(new List<VisitLogStaffViewDto>(), 0, queryParams.PageNumber, queryParams.PageSize);

            query = query.Where(v => allowedBuildingIds.Contains(v.Apartment.BuildingId));

            if (!string.IsNullOrEmpty(queryParams.BuildingId))
            {
                if (!allowedBuildingIds.Contains(queryParams.BuildingId)) return new PagedList<VisitLogStaffViewDto>(new List<VisitLogStaffViewDto>(), 0, queryParams.PageNumber, queryParams.PageSize);
                query = query.Where(v => v.Apartment.BuildingId == queryParams.BuildingId);
            }

            return await ApplyCommonQueryLogic(query, queryParams);
        }

        public async Task<PagedList<VisitLogStaffViewDto>> GetResidentHistoryAsync(VisitorQueryParameters queryParams, string userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.ApartmentId))
            {
                return new PagedList<VisitLogStaffViewDto>(new List<VisitLogStaffViewDto>(), 0, queryParams.PageNumber, queryParams.PageSize);
            }

            var query = _visitLogRepository.GetStaffViewLogsQuery();
            query = query.Where(v => v.ApartmentId == user.ApartmentId);
            return await ApplyCommonQueryLogic(query, queryParams);
        }

        public async Task<IEnumerable<VisitorDto>> GetRecentVisitorsAsync(string apartmentId)
        {
            var visitors = await _visitorRepository.GetRecentVisitorsByApartmentAsync(apartmentId);
            return _mapper.Map<IEnumerable<VisitorDto>>(visitors);
        }

        public async Task<bool> CheckInAsync(string id)
        {
            var visitLog = await _visitLogRepository.FirstOrDefaultAsync(v => v.VisitLogId == id);
            if (visitLog == null || visitLog.Status != "Pending") return false;

            visitLog.Status = "Checked-in";
            visitLog.CheckinTime = DateTime.UtcNow;
            await _visitLogRepository.UpdateAsync(visitLog);
            return await _visitLogRepository.SaveChangesAsync();
        }

        public async Task<bool> CheckOutAsync(string id)
        {
            var visitLog = await _visitLogRepository.FirstOrDefaultAsync(v => v.VisitLogId == id);
            if (visitLog == null || visitLog.Status != "Checked-in") return false;

            visitLog.Status = "Checked-out";
            visitLog.CheckoutTime = DateTime.UtcNow;
            await _visitLogRepository.UpdateAsync(visitLog);
            return await _visitLogRepository.SaveChangesAsync();
        }

        public async Task<bool> DeleteLogAsync(string id)
        {
            return await _visitLogRepository.DeleteAsync(id);
        }

        private async Task<PagedList<VisitLogStaffViewDto>> ApplyCommonQueryLogic(IQueryable<VisitLog> query, VisitorQueryParameters queryParams)
        {
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
                query = isDescending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
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

            return new PagedList<VisitLogStaffViewDto>(items, totalCount, queryParams.PageNumber, queryParams.PageSize);
        }
    }
}