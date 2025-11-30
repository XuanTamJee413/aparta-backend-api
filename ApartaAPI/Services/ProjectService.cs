using ApartaAPI.Data;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Projects;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ApartaAPI.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IRepository<Project> _repository;
        private readonly IRepository<Subscription> _subscriptionRepository;
        private readonly IMapper _mapper;
        private readonly ApartaDbContext _context;

        public ProjectService(
            IRepository<Project> repository,
            IRepository<Subscription> subscriptionRepository,
            IMapper mapper,
            ApartaDbContext context)
        {
            _repository = repository;
            _subscriptionRepository = subscriptionRepository;
            _mapper = mapper;
            _context = context;
        }

        // --- HELPER FUNCTIONS ---
        private string? ConvertToTitleCase(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var cleanInput = Regex.Replace(input.Trim(), @"\s+", " ").ToLower();
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleanInput);
        }

        private string? ConvertToUpperCase(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var str = input.Trim().Replace("đ", "d").Replace("Đ", "D");
            var normalizedString = str.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
            return Regex.Replace(result, @"\s+", " ").ToUpper();
        }

        private string? ValidateProjectLogic(string? projectCode, string? name, string? bankAccountNumber)
        {
            if (!string.IsNullOrEmpty(projectCode))
            {
                if (!Regex.IsMatch(projectCode, "^[A-Z0-9_]+$"))
                    return "Mã dự án không hợp lệ (Chỉ A-Z, 0-9, _).";
                if (projectCode.Length < 2 || projectCode.Length > 50)
                    return "Mã dự án phải từ 2 đến 50 ký tự.";
            }

            if (name != null)
            {
                if (string.IsNullOrWhiteSpace(name)) return "Tên dự án không được để trống.";
                if (name.Trim().Length < 3 || name.Trim().Length > 200) return "Tên dự án phải từ 3 đến 200 ký tự.";
            }

            if (!string.IsNullOrEmpty(bankAccountNumber))
            {
                if (!Regex.IsMatch(bankAccountNumber, "^[0-9]+$"))
                    return "Số tài khoản ngân hàng không hợp lệ (chỉ được chứa số).";
                if (bankAccountNumber.Length < 6 || bankAccountNumber.Length > 25)
                    return "Số tài khoản ngân hàng phải từ 6 đến 25 chữ số.";
            }

            return null;
        }

        // --- GET ALL (Đã sửa logic đếm) ---
        public async Task<ApiResponse<IEnumerable<ProjectDto>>> GetAllAsync(ProjectQueryParameters query)
        {
            try
            {
                var searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm) ? null : query.SearchTerm.Trim().ToLower();
                var queryable = _context.Projects.AsNoTracking();

                if (query.IsActive.HasValue)
                    queryable = queryable.Where(p => p.IsActive == query.IsActive.Value);

                if (searchTerm != null)
                    queryable = queryable.Where(p => p.Name.ToLower().Contains(searchTerm) || p.ProjectCode.ToLower().Contains(searchTerm));

                if (!string.IsNullOrWhiteSpace(query.SortBy))
                {
                    bool isDesc = query.SortOrder?.ToLower() == "desc";
                    switch (query.SortBy.ToLower())
                    {
                        case "numapartments":
                            // Sắp xếp theo số căn "Đã thuê"
                            queryable = isDesc
                                ? queryable.OrderByDescending(p => p.Buildings.SelectMany(b => b.Apartments).Count(a => a.Status == "Đã Thuê"))
                                : queryable.OrderBy(p => p.Buildings.SelectMany(b => b.Apartments).Count(a => a.Status == "Đã Thuê"));
                            break;
                        case "numbuildings":
                            // Sắp xếp theo số tòa "Active"
                            queryable = isDesc
                                ? queryable.OrderByDescending(p => p.Buildings.Count(b => b.IsActive))
                                : queryable.OrderBy(p => p.Buildings.Count(b => b.IsActive));
                            break;
                        default:
                            queryable = isDesc ? queryable.OrderByDescending(p => p.Name) : queryable.OrderBy(p => p.Name);
                            break;
                    }
                }
                else
                {
                    queryable = queryable.OrderByDescending(p => p.CreatedAt);
                }

                // [QUAN TRỌNG] Sửa logic Count trong Select
                var dtos = await queryable
                    .Select(p => new ProjectDto(
                        p.ProjectId,
                        p.ProjectCode,
                        p.Name,
                        p.Address,
                        p.Ward,
                        p.District,
                        p.City,
                        p.BankName,
                        p.BankAccountNumber,
                        p.BankAccountName,
                        // 1. Đếm số căn hộ ĐÃ THUÊ
                        p.Buildings.SelectMany(b => b.Apartments).Count(a => a.Status == "Đã Thuê"),
                        // 2. Đếm số tòa nhà ACTIVE
                        p.Buildings.Count(b => b.IsActive == true),
                        p.CreatedAt,
                        p.UpdatedAt,
                        p.IsActive
                    ))
                    .ToListAsync();

                if (!dtos.Any())
                    return ApiResponse<IEnumerable<ProjectDto>>.Success(new List<ProjectDto>(), ApiResponse.SM01_NO_RESULTS);

                return ApiResponse<IEnumerable<ProjectDto>>.Success(dtos);
            }
            catch (Exception)
            {
                return ApiResponse<IEnumerable<ProjectDto>>.Fail(ApiResponse.SM40_SYSTEM_ERROR);
            }
        }

        // --- GET BY ID (Đã sửa logic đếm) ---
        public async Task<ApiResponse<ProjectDto>> GetByIdAsync(string id)
        {
            try
            {
                var dto = await _context.Projects
                    .AsNoTracking()
                    .Where(p => p.ProjectId == id)
                    .Select(p => new ProjectDto(
                        p.ProjectId,
                        p.ProjectCode,
                        p.Name,
                        p.Address,
                        p.Ward,
                        p.District,
                        p.City,
                        p.BankName,
                        p.BankAccountNumber,
                        p.BankAccountName,
                        // Đếm số căn hộ ĐÃ THUÊ
                        p.Buildings.SelectMany(b => b.Apartments).Count(a => a.Status == "Đã Thuê"),
                        // Đếm số tòa nhà ACTIVE
                        p.Buildings.Count(b => b.IsActive == true),
                        p.CreatedAt,
                        p.UpdatedAt,
                        p.IsActive
                    ))
                    .FirstOrDefaultAsync();

                if (dto == null) return ApiResponse<ProjectDto>.Fail(ApiResponse.SM01_NO_RESULTS);
                return ApiResponse<ProjectDto>.Success(dto);
            }
            catch (Exception)
            {
                return ApiResponse<ProjectDto>.Fail(ApiResponse.SM40_SYSTEM_ERROR);
            }
        }

        // --- CREATE (Giữ nguyên logic Validate & Normalize mới) ---
        public async Task<ApiResponse<ProjectDto>> CreateAsync(ProjectCreateDto dto, string adminId)
        {
            try
            {
                var errorMsg = ValidateProjectLogic(dto.ProjectCode, dto.Name, dto.BankAccountNumber);
                if (errorMsg != null) return ApiResponse<ProjectDto>.Fail(ApiResponse.SM25_INVALID_INPUT, null, errorMsg);

                var codeToCheck = dto.ProjectCode!.Trim();
                var exists = await _repository.FirstOrDefaultAsync(p => p.ProjectCode == codeToCheck);
                if (exists != null) return ApiResponse<ProjectDto>.Fail(ApiResponse.SM16_DUPLICATE_CODE, "ProjectCode");

                var now = DateTime.UtcNow;
                var entity = _mapper.Map<Project>(dto);

                entity.ProjectId = Guid.NewGuid().ToString("N");
                entity.ProjectCode = codeToCheck;

                entity.Name = ConvertToTitleCase(dto.Name) ?? entity.Name;
                entity.Address = ConvertToTitleCase(dto.Address);
                entity.Ward = ConvertToTitleCase(dto.Ward);
                entity.District = ConvertToTitleCase(dto.District);
                entity.City = ConvertToTitleCase(dto.City);
                entity.BankAccountName = ConvertToUpperCase(dto.BankAccountName);

                entity.AdminId = adminId;
                entity.IsActive = true;
                entity.CreatedAt = now;
                entity.UpdatedAt = now;

                await _repository.AddAsync(entity);
                await _repository.SaveChangesAsync();

                return await GetByIdAsync(entity.ProjectId);
            }
            catch (Exception)
            {
                return ApiResponse<ProjectDto>.Fail(ApiResponse.SM40_SYSTEM_ERROR);
            }
        }

        // --- UPDATE (Đã thêm logic Deactivate User & Staff Assignment) ---
        public async Task<ApiResponse> UpdateAsync(string id, ProjectUpdateDto dto)
        {
            try
            {
                var entity = await _repository.FirstOrDefaultAsync(p => p.ProjectId == id);
                if (entity == null) return ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS);

                var errorMsg = ValidateProjectLogic(null, dto.Name, dto.BankAccountNumber);
                if (errorMsg != null) return ApiResponse.Fail(ApiResponse.SM25_INVALID_INPUT, null, errorMsg);

                // === LOGIC DEACTIVATE ===
                bool isDeactivating = dto.IsActive.HasValue && dto.IsActive.Value == false && entity.IsActive == true;

                if (isDeactivating)
                {
                    var now = DateTime.UtcNow;

                    // 1. Hủy Subscription
                    var activeSub = await _subscriptionRepository.FirstOrDefaultAsync(
                        s => s.ProjectId == id && s.Status == "Active" && s.ExpiredAt > now
                    );
                    if (activeSub != null)
                    {
                        activeSub.Status = "Cancelled";
                        activeSub.ExpiredAt = now;
                        activeSub.UpdatedAt = now;
                    }

                    // 2. Vô hiệu hóa Cư dân (User linked to Apartment in Project)
                    // Tìm user có ApartmentId thuộc về Building thuộc Project này
                    var residentUsers = await _context.Users
                        .Where(u => u.Apartment != null && u.Apartment.Building.ProjectId == id && u.Status == "Active")
                        .ToListAsync();

                    foreach (var user in residentUsers)
                    {
                        user.Status = "Inactive";
                        user.UpdatedAt = now;
                    }

                    // 3. Vô hiệu hóa Phân công nhân viên (StaffBuildingAssignment)
                    // Tìm assignment thuộc Building của Project này và đang Active
                    var staffAssignments = await _context.StaffBuildingAssignments
                        .Where(sba => sba.Building.ProjectId == id && sba.IsActive == true)
                        .ToListAsync();

                    foreach (var assignment in staffAssignments)
                    {
                        assignment.IsActive = false;
                        assignment.AssignmentEndDate = DateOnly.FromDateTime(now);
                        assignment.UpdatedAt = now;
                    }
                }

                _mapper.Map(dto, entity);

                if (dto.Name != null) entity.Name = ConvertToTitleCase(dto.Name) ?? entity.Name;
                if (dto.Address != null) entity.Address = ConvertToTitleCase(dto.Address);
                if (dto.Ward != null) entity.Ward = ConvertToTitleCase(dto.Ward);
                if (dto.District != null) entity.District = ConvertToTitleCase(dto.District);
                if (dto.City != null) entity.City = ConvertToTitleCase(dto.City);
                if (dto.BankAccountName != null) entity.BankAccountName = ConvertToUpperCase(dto.BankAccountName);

                entity.UpdatedAt = DateTime.UtcNow;

                // Lưu tất cả thay đổi (Project, Subscription, Users, Assignments) trong 1 transaction
                await _repository.UpdateAsync(entity);
                await _repository.SaveChangesAsync(); // SaveChanges của _context sẽ lưu tất cả tracker

                return ApiResponse.Success(ApiResponse.SM03_UPDATE_SUCCESS);
            }
            catch (Exception)
            {
                return ApiResponse.Fail(ApiResponse.SM40_SYSTEM_ERROR);
            }
        }
    }
}