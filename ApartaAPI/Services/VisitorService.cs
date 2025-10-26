﻿using ApartaAPI.DTOs.Visitors;
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

        // checked in = staff gui len, pending hoac 0 co j thi la resident gui len
        public async Task<VisitorDto> CreateVisitAsync(VisitorCreateDto dto)
        {
            var apartmentExists = await _apartmentRepository.FirstOrDefaultAsync(a => a.ApartmentId == dto.ApartmentId || a.Code == dto.ApartmentId);
            if (apartmentExists == null)
            {
                throw new ValidationException($"Căn hộ với ID '{dto.ApartmentId}' không tồn tại.");
            }

            var newVisitor = _mapper.Map<Visitor>(dto);
            newVisitor.VisitorId = Guid.NewGuid().ToString("N");
            await _visitorRepository.AddAsync(newVisitor);

            var newVisitLog = _mapper.Map<VisitLog>(dto);
            newVisitLog.VisitLogId = Guid.NewGuid().ToString("N");
            newVisitLog.VisitorId = newVisitor.VisitorId;
            newVisitLog.ApartmentId = apartmentExists.ApartmentId;
            newVisitLog.CheckoutTime = null;

            if (dto.Status == "Pending")
            {
                if (DateTime.TryParse(dto.CheckinTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var checkinTime))
                {
                    newVisitLog.CheckinTime = checkinTime.ToUniversalTime();
                }
                else
                {
                    throw new ValidationException("Định dạng thời gian check-in không hợp lệ.");
                }
                newVisitLog.Status = "Pending";
            }
            else
            {
                newVisitLog.CheckinTime = DateTime.UtcNow;
                newVisitLog.Status = "Checked-in";
            }

            await _visitLogRepository.AddAsync(newVisitLog);
            await _visitorRepository.SaveChangesAsync();

            return _mapper.Map<VisitorDto>(newVisitor);
        }
    }
}
