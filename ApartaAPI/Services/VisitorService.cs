using ApartaAPI.DTOs.Visitors;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ApartaAPI.Services
{
    public class VisitorService : IVisitorService
    {
        private readonly IVisitorRepository _visitorRepository;
        private readonly IVisitLogRepository _visitLogRepository;
        private readonly IRepository<Apartment> _apartmentRepository;
        private readonly IMapper _mapper;

        public VisitorService(
            IVisitorRepository visitorRepository,
            IVisitLogRepository visitLogRepository,
            IRepository<Apartment> apartmentRepository,
            IMapper mapper)
        {
            _visitorRepository = visitorRepository;
            _visitLogRepository = visitLogRepository;
            _apartmentRepository = apartmentRepository;
            _mapper = mapper;
        }

        public async Task<VisitorDto> CreateVisitAsync(VisitorCreateDto dto)
        {
            // 1. Kiểm tra căn hộ
            var apartmentExists = await _apartmentRepository.FirstOrDefaultAsync(a => a.ApartmentId == dto.ApartmentId || a.Code == dto.ApartmentId);
            if (apartmentExists == null)
            {
                throw new ValidationException($"Căn hộ với ID '{dto.ApartmentId}' không tồn tại.");
            }

            // --- [SỬA LOGIC A2.1: TÁI SỬ DỤNG KHÁCH CŨ] ---
            var visitor = await _visitorRepository.FirstOrDefaultAsync(v => v.IdNumber == dto.IdNumber);

            if (visitor == null)
            {
                // Nếu chưa có -> Tạo mới
                visitor = _mapper.Map<Visitor>(dto);
                visitor.VisitorId = Guid.NewGuid().ToString("N");
                await _visitorRepository.AddAsync(visitor);
            }
            else
            {
                // Nếu đã có -> Cập nhật thông tin mới nhất (SĐT, Tên có thể thay đổi)
                visitor.FullName = dto.FullName;
                visitor.Phone = dto.Phone;
                // Không tạo ID mới, dùng lại ID cũ
                await _visitorRepository.UpdateAsync(visitor);
            }

            // 2. Xử lý VisitLog
            var newVisitLog = _mapper.Map<VisitLog>(dto);
            newVisitLog.VisitLogId = Guid.NewGuid().ToString("N");
            newVisitLog.VisitorId = visitor.VisitorId; // Luôn dùng ID của visitor (dù mới hay cũ)
            newVisitLog.ApartmentId = apartmentExists.ApartmentId;
            newVisitLog.CheckoutTime = null;

            // 3. Xử lý thời gian (BR-21 & BR-126)
            if (string.IsNullOrWhiteSpace(dto.CheckinTime))
            {
                // BR-21: Nếu để trống, mặc định 1 ngày từ hiện tại
                newVisitLog.CheckinTime = DateTime.UtcNow.AddDays(1);
                newVisitLog.Status = "Pending";
            }
            else
            {
                if (DateTime.TryParse(dto.CheckinTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedTime))
                {
                    var utcTime = parsedTime.ToUniversalTime();

                    // BR-126: Cannot be in the past (cho phép sai số nhỏ 5 phút)
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

                // Nếu Resident tạo thì luôn là Pending, trừ khi Staff tạo mới cho Check-in ngay
                newVisitLog.Status = dto.Status ?? "Pending";
            }

            await _visitLogRepository.AddAsync(newVisitLog);

            // SaveChanges 1 lần cuối cùng để đảm bảo Transaction
            await _visitorRepository.SaveChangesAsync();

            return _mapper.Map<VisitorDto>(visitor);
        }

        // Thêm hàm lấy khách cũ cho Alternative Flow 2
        public async Task<IEnumerable<VisitorDto>> GetRecentVisitorsAsync(string apartmentId)
        {
            var visitors = await _visitorRepository.GetRecentVisitorsByApartmentAsync(apartmentId);
            return _mapper.Map<IEnumerable<VisitorDto>>(visitors);
        }
    }
}
