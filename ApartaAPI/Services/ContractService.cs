using ApartaAPI.Data;
using ApartaAPI.DTOs;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Contracts;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;

namespace ApartaAPI.Services
{
    public class ContractService : IContractService
    {
        private readonly IRepository<Contract> _contractRepository;
        private readonly IRepository<Apartment> _apartmentRepository;
        private readonly IRepository<ApartmentMember> _apartmentMemberRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Role> _roleRepository; // Thay thế RoleManager
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMapper _mapper;
        private readonly ApartaDbContext _context;

        private readonly IMailService _mailService;
        private readonly IConfiguration _configuration;

        public ContractService(
            ApartaDbContext context,
            IRepository<Contract> contractRepository,
            IRepository<Apartment> apartmentRepository,
            IRepository<ApartmentMember> apartmentMemberRepository,
            IRepository<User> userRepository,
            IRepository<Role> roleRepository, // Inject Repo thay vì Manager
            ICloudinaryService cloudinaryService,
            IMapper mapper,
            IMailService mailService,
            IConfiguration configuration)
        {
            _context = context;
            _contractRepository = contractRepository;
            _apartmentRepository = apartmentRepository;
            _apartmentMemberRepository = apartmentMemberRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _cloudinaryService = cloudinaryService;
            _mapper = mapper;

            _mailService = mailService;
            _configuration = configuration;
        }

        public async Task<ApiResponse<IEnumerable<ContractDto>>> GetAllAsync(ContractQueryParameters query)
        {
            if (query == null)
            {
                query = new ContractQueryParameters(null, null, null);
            }

            var apartmentIdFilter = string.IsNullOrWhiteSpace(query.ApartmentId)
                ? null
                : query.ApartmentId.Trim();

            Expression<Func<Contract, bool>> predicate = c =>
                (apartmentIdFilter == null || c.ApartmentId == apartmentIdFilter);

            var entities = await _contractRepository.FindAsync(predicate);
            if (entities == null || !entities.Any())
            {
                return ApiResponse<IEnumerable<ContractDto>>.Success(new List<ContractDto>(), ApiResponse.SM01_NO_RESULTS);
            }

            var validEntities = entities.Where(e => e != null).ToList();

            IOrderedEnumerable<Contract> sortedEntities;
            bool isDescending = query.SortOrder?.ToLowerInvariant() == "desc";

            switch (query.SortBy?.ToLowerInvariant())
            {
                case "startdate":
                    sortedEntities = isDescending
                        ? validEntities.OrderByDescending(c => c.StartDate)
                        : validEntities.OrderBy(c => c.StartDate);
                    break;
                case "enddate":
                    sortedEntities = isDescending
                        ? validEntities.OrderByDescending(c => c.EndDate)
                        : validEntities.OrderBy(c => c.EndDate);
                    break;
                default:
                    sortedEntities = validEntities.OrderByDescending(c => c.CreatedAt);
                    break;
            }

            var sortedList = sortedEntities.ToList();

            var apartmentIds = sortedList
                .Select(c => c.ApartmentId)
                .Distinct()
                .ToList();

            var apartments = await _apartmentRepository.FindAsync(a => apartmentIds.Contains(a.ApartmentId));
            var apartmentDict = (apartments ?? Enumerable.Empty<Apartment>())
                .GroupBy(a => a.ApartmentId)
                .ToDictionary(g => g.Key, g => g.First());

            var ownerMembers = await _apartmentMemberRepository.FindAsync(
                m => apartmentIds.Contains(m.ApartmentId)
                     && m.IsOwner == true
                     && (m.Status == "Đã Bán" || m.Status == "Đang cư trú")
            );
            var ownerDict = (ownerMembers ?? Enumerable.Empty<ApartmentMember>())
                .GroupBy(m => m.ApartmentId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.CreatedAt).FirstOrDefault()
                );

            var users = await _userRepository.FindAsync(
                u => apartmentIds.Contains(u.ApartmentId ?? "") && !u.IsDeleted
            );
            var userDict = (users ?? Enumerable.Empty<User>())
                .GroupBy(u => u.ApartmentId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.CreatedAt).FirstOrDefault()
                );

            var dtos = sortedList.Select(c =>
            {
                apartmentDict.TryGetValue(c.ApartmentId, out var apt);
                ownerDict.TryGetValue(c.ApartmentId, out var owner);
                userDict.TryGetValue(c.ApartmentId, out var user);

                var displayName = owner?.Name ?? user?.Name;
                var displayPhone = owner?.PhoneNumber ?? user?.Phone;
                var displayEmail = user?.Email;

                return new ContractDto
                {
                    ContractId = c.ContractId,
                    ApartmentId = c.ApartmentId,
                    ApartmentCode = apt?.Code,
                    OwnerName = displayName,
                    OwnerPhoneNumber = displayPhone,
                    OwnerEmail = displayEmail,
                    Image = c.Image,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    CreatedAt = c.CreatedAt,
                    ContractType = c.ContractType,
                    DepositAmount = c.DepositAmount,
                    TotalValue = c.TotalValue
                };
            }).ToList();

            return ApiResponse<IEnumerable<ContractDto>>.Success(dtos);
        }

        public async Task<ContractDto?> GetByIdAsync(string id)
        {
            var contract = await _contractRepository.FirstOrDefaultAsync(c => c.ContractId == id);
            if (contract == null)
                return null;

            var apartment = await _apartmentRepository.FirstOrDefaultAsync(a => a.ApartmentId == contract.ApartmentId);

            var ownerMembers = await _apartmentMemberRepository.FindAsync(
                m => m.ApartmentId == contract.ApartmentId
                     && m.IsOwner == true
                     && (m.Status == "Đã Bán" || m.Status == "Đang cư trú")
            );
            var owner = (ownerMembers ?? Enumerable.Empty<ApartmentMember>())
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefault();

            var users = await _userRepository.FindAsync(
                u => u.ApartmentId == contract.ApartmentId && !u.IsDeleted
            );
            var user = (users ?? Enumerable.Empty<User>())
                .OrderByDescending(u => u.CreatedAt)
                .FirstOrDefault();

            var displayName = owner?.Name ?? user?.Name;
            var displayPhone = owner?.PhoneNumber ?? user?.Phone;
            var displayEmail = user?.Email;

            var dto = new ContractDto
            {
                ContractId = contract.ContractId,
                ApartmentId = contract.ApartmentId,
                ApartmentCode = apartment?.Code,
                OwnerName = displayName,
                OwnerPhoneNumber = displayPhone,
                OwnerEmail = displayEmail,
                Image = contract.Image,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                CreatedAt = contract.CreatedAt,
                ContractType = contract.ContractType,
                DepositAmount = contract.DepositAmount,
                TotalValue = contract.TotalValue
            };

            return dto;
        }

        public async Task<ApiResponse<ContractDto>> CreateAsync(CreateContractRequestDto request)
        {
            // 1. VALIDATION LAYER (Giữ nguyên)
            var apartment = await _apartmentRepository.GetByIdAsync(request.ApartmentId);
            if (apartment == null)
                return ApiResponse<ContractDto>.Fail(ApiResponse.SM58_APARTMENT_NOT_FOUND);

            var existingContract = await _contractRepository.FirstOrDefaultAsync(c => c.ContractNumber == request.ContractNumber);
            if (existingContract != null)
                return ApiResponse<ContractDto>.Fail(ApiResponse.SM59_CONTRACT_NUMBER_EXISTS);

            var startDateOnly = request.StartDate;
            var endDateOnly = request.EndDate;

            if (startDateOnly > endDateOnly)
                return ApiResponse<ContractDto>.Fail(ApiResponse.SM60_INVALID_CONTRACT_DATES);

            foreach (var mem in request.Members.Where(m => m.IsAppAccess))
            {
                if (string.IsNullOrWhiteSpace(mem.PhoneNumber))
                    return ApiResponse<ContractDto>.Fail(ApiResponse.SM61_MEMBER_PHONE_REQUIRED, mem.FullName);

                if (!string.IsNullOrEmpty(mem.PhoneNumber))
                {
                    // Regex giải thích:
                    // ^           : Bắt đầu chuỗi
                    // (\+84|0)    : Phải bắt đầu bằng "+84" HOẶC số "0"
                    // \d{9,11}    : Theo sau là 9 đến 11 chữ số
                    // $           : Kết thúc chuỗi
                    // => Tổng độ dài chấp nhận: 10 số (0xxxxxxxxx) đến 12-13 ký tự (+84xxxxxxxxx)
                    if (!System.Text.RegularExpressions.Regex.IsMatch(mem.PhoneNumber, @"^(\+84|0)\d{9,11}$"))
                    {
                        return ApiResponse<ContractDto>.Fail(ApiResponse.SM25_INVALID_INPUT, "PhoneNumber",
                            $"{mem.FullName}: Số điện thoại không đúng định dạng (VD: 09xx... hoặc +849xx...)");
                    }
                }
            }

            // 2. EXECUTION LAYER
            using var transaction = await _context.Database.BeginTransactionAsync();

            var emailTasks = new List<Func<System.Threading.Tasks.Task>>();
            try
            {
                // A. TẠO HỢP ĐỒNG
                var contract = new Contract
                {
                    ContractId = Guid.NewGuid().ToString("N"),
                    ApartmentId = request.ApartmentId,
                    ContractNumber = request.ContractNumber,
                    ContractType = request.ContractType,
                    StartDate = startDateOnly,
                    EndDate = endDateOnly,
                    DepositAmount = 0,
                    Status = "Đang hiệu lực",
                    TotalValue = 0,
                    CreatedAt = DateTime.UtcNow
                };

                // Repo.AddAsync đã tự gọi SaveChangesAsync(), nên lệnh INSERT được bắn vào Transaction ngay tại đây
                await _contractRepository.AddAsync(contract);

                string? representativeMemberId = null;
                string? headMemberId = null;

                // B. XỬ LÝ CƯ DÂN & USER
                var sortedMembers = request.Members.OrderByDescending(m => m.IsRepresentative).ToList();

                foreach (var memDto in sortedMembers)
                {
                    string? userId = null;

                    // B.1 Tạo User
                    if (memDto.IsAppAccess && !string.IsNullOrEmpty(memDto.PhoneNumber))
                    {
                        var user = await _userRepository.FirstOrDefaultAsync(u => u.Phone == memDto.PhoneNumber);

                        if (user == null)
                        {
                            var sysRole = await _roleRepository.FirstOrDefaultAsync(r => r.RoleName == "resident");
                            if (sysRole == null)
                            {
                                await transaction.RollbackAsync();
                                return ApiResponse<ContractDto>.Fail(ApiResponse.SM64_SYSTEM_ROLE_MISSING);
                            }

                            string defaultPassword = _configuration["Authentication:DefaultResidentPassword"] ?? "Aparta@123";
                            user = new User
                            {
                                UserId = Guid.NewGuid().ToString(),
                                RoleId = sysRole.RoleId,
                                ApartmentId = request.ApartmentId,
                                Phone = memDto.PhoneNumber,
                                Email = memDto.Email,
                                Name = memDto.FullName,
                                PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
                                Status = "active",
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                IsDeleted = false,
                                IsFirstLogin = true
                            };
                            // Repo.AddAsync tự save
                            await _userRepository.AddAsync(user);

                            if (!string.IsNullOrWhiteSpace(memDto.Email))
                            {
                                var email = memDto.Email;
                                var fullName = memDto.FullName;
                                var phone = memDto.PhoneNumber;
                                var emailPassword = defaultPassword;
                                emailTasks.Add(() => SendWelcomeEmailAsync(email, fullName, phone, emailPassword));
                            }
                        }
                        userId = user.UserId;
                    }

                    // B.2 Tìm Role Ngữ Cảnh
                    var roleNameLower = memDto.RoleName.ToLower();
                    var contextRole = await _roleRepository.FirstOrDefaultAsync(r => r.RoleName == roleNameLower);

                    if (contextRole == null)
                    {
                        await transaction.RollbackAsync();
                        return ApiResponse<ContractDto>.Fail(ApiResponse.SM63_ROLE_NOT_FOUND, memDto.RoleName);
                    }

                    // B.3 Tạo Member
                    var member = new ApartmentMember
                    {
                        ApartmentMemberId = Guid.NewGuid().ToString("N"),
                        ApartmentId = request.ApartmentId,
                        Name = memDto.FullName,
                        PhoneNumber = memDto.PhoneNumber,
                        IdNumber = memDto.IdentityCard,
                        UserId = userId,
                        RoleId = contextRole.RoleId,
                        IsAppAccess = memDto.IsAppAccess,
                        Status = "Đang cư trú",
                        HeadMemberId = memDto.IsRepresentative ? null : headMemberId,
                        IsOwner = roleNameLower == "owner",
                        CreatedAt = DateTime.UtcNow
                    };

                    // Repo.AddAsync tự save -> Insert Member vào Transaction
                    await _apartmentMemberRepository.AddAsync(member);

                    if (memDto.IsRepresentative)
                    {
                        representativeMemberId = member.ApartmentMemberId;
                        headMemberId = member.ApartmentMemberId;
                    }
                }

                // C. CẬP NHẬT CONTRACT & APARTMENT
                contract.RepresentativeMemberId = representativeMemberId;
                // Repo.UpdateAsync tự save
                await _contractRepository.UpdateAsync(contract);

                if (request.ContractType == "Sale") apartment.Status = "Đã Bán";
                else if (request.ContractType == "Lease" && apartment.Status == "Còn Trống") apartment.Status = "Đang Thuê";

                apartment.OccupancyStatus = "Đã có người ở";
                if (apartment.HandoverDate == null) apartment.HandoverDate = DateOnly.FromDateTime(DateTime.Now);

                // Repo.UpdateAsync tự save
                await _apartmentRepository.UpdateAsync(apartment);

                // D. COMMIT
                await transaction.CommitAsync();

                foreach (var emailTask in emailTasks)
                {
                    _ = System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            await emailTask();
                        }
                        catch
                        {
                        }
                    });
                }

                var resultDto = _mapper.Map<ContractDto>(contract);
                return ApiResponse<ContractDto>.Success(resultDto, ApiResponse.GetMessageFromCode(ApiResponse.SM04_CREATE_SUCCESS).Replace("{objectName}", "Hợp đồng"));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ApiResponse<ContractDto>.Fail(ApiResponse.SM40_SYSTEM_ERROR, customMessage: ex.Message);
            }
        }

        private async System.Threading.Tasks.Task SendWelcomeEmailAsync(string toEmail, string name, string phone, string password)
        {
            var frontendUrl = _configuration["Environment:FrontendUrl"] ?? "http://localhost:4200";
            var subject = "Chào mừng cư dân mới - Aparta System";

            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #4F46E5;'>Chào mừng đến với Aparta!</h2>
                    <p>Xin chào <strong>{name}</strong>,</p>
                    <p>Tài khoản cư dân của bạn đã được tạo thành công.</p>
                    <p>Thông tin đăng nhập:</p>
                    <ul>
                        <li><strong>Số điện thoại:</strong> {phone}</li>
                        <li><strong>Mật khẩu tạm thời:</strong> {password}</li>
                    </ul>
                    <p>Vui lòng đăng nhập và đổi mật khẩu ngay trong lần đầu tiên.</p>
                    <a href='{frontendUrl}/login' style='background-color: #4F46E5; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block; margin-top: 10px;'>Đăng nhập ngay</a>
                    <hr style='margin-top: 20px; border: 0; border-top: 1px solid #eee;' />
                    <p style='font-size: 12px; color: #666;'>Đây là email tự động, vui lòng không trả lời.</p>
                </div>";

            await _mailService.SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> UpdateAsync(string id, ContractUpdateDto dto)
        {
            var entity = await _contractRepository.FirstOrDefaultAsync(c => c.ContractId == id);
            if (entity == null)
                return false;

            if (dto.EndDate.HasValue)
            {
                if (entity.StartDate.HasValue && dto.EndDate.Value < entity.StartDate.Value)
                {
                    throw new InvalidOperationException("Ngày kết thúc không được nhỏ hơn ngày bắt đầu hợp đồng.");
                }

                entity.EndDate = dto.EndDate;
            }

            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                var uploadResult = await _cloudinaryService.UploadImageAsync(dto.ImageFile, "contracts");
                entity.Image = uploadResult.SecureUrl;
            }
            else if (dto.Image != null)
            {
                entity.Image = string.IsNullOrWhiteSpace(dto.Image)
                    ? null
                    : dto.Image;
            }

            entity.UpdatedAt = DateTime.UtcNow;

            await _contractRepository.UpdateAsync(entity);
            return await _contractRepository.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var contract = await _contractRepository.FirstOrDefaultAsync(c => c.ContractId == id);
            if (contract == null)
                return false;

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (!contract.EndDate.HasValue || contract.EndDate.Value > today)
            {
                throw new InvalidOperationException("Hợp đồng chưa hết hạn, không được phép xóa.");
            }

            var apartment = await _apartmentRepository
                .FirstOrDefaultAsync(a => a.ApartmentId == contract.ApartmentId);

            if (apartment.Status == "Đã Đóng") return false;

            var ownerMembers = await _apartmentMemberRepository.FindAsync(
                m => m.ApartmentId == contract.ApartmentId
                     && m.IsOwner == true
                     && m.Status == "Đang cư trú"
            );

            var now = DateTime.UtcNow;

            if (apartment != null)
            {
                var originalCode = apartment.Code;

                apartment.Status = "Đã Đóng";
                apartment.Code = $"{originalCode}-HIS-{now:yyyyMMdd}";
                apartment.UpdatedAt = now;

                if (ownerMembers != null)
                {
                    foreach (var member in ownerMembers)
                    {
                        member.Status = "Đã rời đi";
                        member.IsOwner = false;
                        member.UpdatedAt = now;
                    }
                }

                var newApartment = new Apartment
                {
                    ApartmentId = Guid.NewGuid().ToString("N"),
                    BuildingId = apartment.BuildingId,
                    Code = originalCode,
                    Type = apartment.Type,
                    Status = "Còn Trống",
                    OccupancyStatus = "Chưa có người ở",
                    Area = apartment.Area,
                    Floor = apartment.Floor,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _apartmentRepository.AddAsync(newApartment);
            }

            await _contractRepository.SaveChangesAsync();
            return true;
        }

        public async Task<ApiResponse<IEnumerable<ContractDto>>> GetByUserBuildingsAsync(string userId, ContractQueryParameters query)
        {
            if (query == null)
            {
                query = new ContractQueryParameters(null, null, null);
            }

            var apartmentIdFilter = string.IsNullOrWhiteSpace(query.ApartmentId)
                ? null
                : query.ApartmentId.Trim();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // filter: hợp đồng mà căn hộ thuộc building mà user được gán (và assignment đang active)
            Expression<Func<Contract, bool>> predicate = c =>
                c.Apartment != null &&
                c.Apartment.Building.StaffBuildingAssignments.Any(sba =>
                    sba.UserId == userId &&
                    sba.IsActive &&
                    (sba.AssignmentEndDate == null || sba.AssignmentEndDate >= today)
                )
                && (apartmentIdFilter == null || c.ApartmentId == apartmentIdFilter);

            var entities = await _contractRepository.FindAsync(predicate);

            if (entities == null)
            {
                entities = new List<Contract>();
            }

            var validEntities = entities.Where(e => e != null).ToList();

            if (!validEntities.Any())
            {
                return ApiResponse<IEnumerable<ContractDto>>.Success(
                    new List<ContractDto>(),
                    ApiResponse.SM01_NO_RESULTS
                );
            }

            IOrderedEnumerable<Contract> sortedEntities;
            bool isDescending = query.SortOrder?.ToLowerInvariant() == "desc";

            switch (query.SortBy?.ToLowerInvariant())
            {
                case "startdate":
                    sortedEntities = isDescending
                        ? validEntities.OrderByDescending(c => c.StartDate)
                        : validEntities.OrderBy(c => c.StartDate);
                    break;
                case "enddate":
                    sortedEntities = isDescending
                        ? validEntities.OrderByDescending(c => c.EndDate)
                        : validEntities.OrderBy(c => c.EndDate);
                    break;
                default:
                    sortedEntities = validEntities.OrderByDescending(c => c.CreatedAt);
                    break;
            }

            var sortedList = sortedEntities.ToList();

            var apartmentIds = sortedList
                .Select(c => c.ApartmentId)
                .Distinct()
                .ToList();

            var apartments = await _apartmentRepository.FindAsync(a => apartmentIds.Contains(a.ApartmentId));
            var apartmentDict = (apartments ?? Enumerable.Empty<Apartment>())
                .GroupBy(a => a.ApartmentId)
                .ToDictionary(g => g.Key, g => g.First());

            var ownerMembers = await _apartmentMemberRepository.FindAsync(
                m => apartmentIds.Contains(m.ApartmentId)
                     && m.IsOwner == true
                     && (m.Status == "Đang cư trú")
            );
            var ownerDict = (ownerMembers ?? Enumerable.Empty<ApartmentMember>())
                .GroupBy(m => m.ApartmentId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.CreatedAt).FirstOrDefault()
                );

            var users = await _userRepository.FindAsync(
                u => apartmentIds.Contains(u.ApartmentId) && !u.IsDeleted
            );
            var userDict = (users ?? Enumerable.Empty<User>())
                .GroupBy(u => u.ApartmentId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.CreatedAt).FirstOrDefault()
                );

            var dtos = sortedList.Select(c =>
            {
                apartmentDict.TryGetValue(c.ApartmentId, out var apt);
                ownerDict.TryGetValue(c.ApartmentId, out var owner);
                userDict.TryGetValue(c.ApartmentId, out var user);

                var displayName = owner?.Name ?? user?.Name;
                var displayPhone = owner?.PhoneNumber ?? user?.Phone;
                var displayEmail = user?.Email;

                return new ContractDto
                {
                    ContractId = c.ContractId,
                    ApartmentId = c.ApartmentId,
                    ApartmentCode = apt?.Code,
                    OwnerName = displayName,
                    OwnerPhoneNumber = displayPhone,
                    OwnerEmail = displayEmail,
                    Image = c.Image,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    CreatedAt = c.CreatedAt,
                    ContractType = c.ContractType,
                    DepositAmount = c.DepositAmount,
                    TotalValue = c.TotalValue
                };
            }).ToList();

            return ApiResponse<IEnumerable<ContractDto>>.Success(dtos);
        }

    }
}
