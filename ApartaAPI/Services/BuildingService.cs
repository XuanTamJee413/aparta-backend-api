/* --- File: ApartaAPI/Services/BuildingService.cs --- */
using ApartaAPI.Data;
using ApartaAPI.DTOs.Apartments;
using ApartaAPI.DTOs.Buildings;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace ApartaAPI.Services
{
    public class BuildingService : IBuildingService
    {
        private readonly IRepository<Building> _repository;
        private readonly IRepository<Project> _projectRepository;
        private readonly IApartmentService _apartmentService;
        private readonly IMapper _mapper;
        private readonly ApartaDbContext _context;

        public BuildingService(
            IRepository<Building> repository,
            IRepository<Project> projectRepository,
            IApartmentService apartmentService,
            IMapper mapper,
            ApartaDbContext context)
        {
            _repository = repository;
            _projectRepository = projectRepository;
            _apartmentService = apartmentService;
            _mapper = mapper;
            _context = context;
        }

        // --- 1. VALIDATE LOGIC ---
        private string? ValidateBuildingLogic(
            string? buildingCode,
            string? name,
            int? floors,
            int? basements,
            int? startDay,
            int? endDay,
            DateOnly? handoverDate,
            double? totalArea,
            string? receptionPhone)
        {
            // Validate Format Mã tòa nhà
            if (!string.IsNullOrEmpty(buildingCode))
            {
                if (!Regex.IsMatch(buildingCode, "^[A-Z0-9_]+$"))
                {
                    return "Mã tòa nhà không hợp lệ. Chỉ chấp nhận chữ in hoa (A-Z), số (0-9) và gạch dưới (_).";
                }
            }

            // Validate Tên tòa nhà
            if (name != null)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return "Tên tòa nhà không được để trống.";
                var cleanName = name.Trim();
                if (cleanName.Length < 3 || cleanName.Length > 100)
                    return "Tên tòa nhà phải từ 3 đến 100 ký tự.";
            }

            // Validate Số tầng
            if (floors.HasValue && (floors < 1 || floors > 200))
                return "Số tầng nổi phải từ 1 đến 200.";
            // Validate Số hầm
            if (basements.HasValue && (basements < 0 || basements > 10))
                return "Số tầng hầm phải từ 0 đến 10.";
            // Validate Diện tích
            if (totalArea.HasValue && totalArea < 0)
                return "Tổng diện tích không được là số âm.";
            // Validate Hotline (Reception Phone)
            if (!string.IsNullOrEmpty(receptionPhone))
            {
                if (!Regex.IsMatch(receptionPhone, "^[0-9]+$"))
                    return "Hotline chỉ được chứa ký tự số.";
                if (receptionPhone.Length < 8 || receptionPhone.Length > 15)
                    return "Hotline phải có độ dài từ 8 đến 15 số.";
            }

            // Validate Ngày bàn giao
            if (handoverDate.HasValue)
            {
                if (handoverDate.Value.Year < 1990)
                    return "Ngày bàn giao không hợp lệ (Phải từ năm 1990 trở đi).";
                if (handoverDate.Value.Year > DateTime.Now.Year + 50)
                    return "Ngày bàn giao không hợp lệ (Quá xa trong tương lai).";
            }

            // Validate Ngày chốt số
            if (startDay.HasValue && (startDay < 1 || startDay > 31))
                return "Ngày bắt đầu chốt số không hợp lệ (1-31).";
            if (endDay.HasValue && (endDay < 1 || endDay > 31))
                return "Ngày kết thúc chốt số không hợp lệ (1-31).";

            // Validate Logic: Ngày kết thúc phải = Ngày bắt đầu + 2 (hoặc = 1 nếu vượt quá 31)
            if (startDay.HasValue && endDay.HasValue)
            {
                int expectedEnd = startDay.Value + 2;
                if (expectedEnd > 31)
                {
                    expectedEnd = 1; // Sang tháng sau
                }
                
                if (endDay.Value != expectedEnd)
                {
                    return $"Ngày kết thúc chốt số phải bằng {expectedEnd} (Ngày bắt đầu + 2).";
                }
            }

            return null; // Hợp lệ
        }

        public async Task<ApiResponse<PaginatedResult<BuildingDto>>> GetAllAsync(BuildingQueryParameters query)
        {
            try
            {
                var searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm) ? null : query.SearchTerm.Trim().ToLower();
                var queryable = _context.Buildings.AsNoTracking();

                // 1. Filter: Theo ProjectId
                if (!string.IsNullOrWhiteSpace(query.ProjectId))
                {
                    queryable = queryable.Where(b => b.ProjectId == query.ProjectId);
                }

                // 2. Filter: Theo IsActive
                if (query.IsActive.HasValue)
                {
                    queryable = queryable.Where(b => b.IsActive == query.IsActive.Value);
                }

                // 3. Search: Theo tên hoặc mã
                if (searchTerm != null)
                {
                    queryable = queryable.Where(b => b.Name.ToLower().Contains(searchTerm)
                                               || b.BuildingCode.ToLower().Contains(searchTerm));
                }

                // 4. Sorting (Sắp xếp)
                bool isDesc = query.SortOrder?.ToLower() == "desc";
                IOrderedQueryable<Building> orderedQuery;
                if (!string.IsNullOrWhiteSpace(query.SortBy))
                {
                    switch (query.SortBy.ToLower())
                    {
                        case "buildingcode":
                            orderedQuery = isDesc ? queryable.OrderByDescending(b => b.BuildingCode) : queryable.OrderBy(b => b.BuildingCode);
                            break;
                        case "name":
                            orderedQuery = isDesc ? queryable.OrderByDescending(b => b.Name) : queryable.OrderBy(b => b.Name);
                            break;
                        case "totalfloors":
                            orderedQuery = isDesc ? queryable.OrderByDescending(b => b.TotalFloors) : queryable.OrderBy(b => b.TotalFloors);
                            break;
                        case "handoverdate":
                            orderedQuery = isDesc ? queryable.OrderByDescending(b => b.HandoverDate) : queryable.OrderBy(b => b.HandoverDate);
                            break;
                        default:
                            orderedQuery = isDesc ? queryable.OrderByDescending(b => b.CreatedAt) : queryable.OrderBy(b => b.CreatedAt);
                            break;
                    }
                }
                else
                {
                    orderedQuery = queryable.OrderByDescending(b => b.CreatedAt);
                }

                queryable = orderedQuery.ThenByDescending(b => b.CreatedAt);

                var totalCount = await queryable.CountAsync();
                var items = await queryable
                    .Skip(query.Skip)
                    .Take(query.Take)
                    .Select(b => new BuildingDto(
                        b.BuildingId,
                        b.ProjectId,
                        b.BuildingCode,
                        b.Name,
                        b.Apartments.Count(),
                        b.Apartments.SelectMany(a => a.ApartmentMembers).Count(),
                        b.TotalFloors,
                        b.TotalBasements,
                        b.TotalArea,
                        b.HandoverDate,
                        (b.HandoverDate.HasValue && b.HandoverDate.Value.AddYears(5) >= DateOnly.FromDateTime(DateTime.Now))
                            ? "Còn bảo hành" : "Hết bảo hành",
                        b.ReceptionPhone,
                        b.Description,
                        b.ReadingWindowStart,
                        b.ReadingWindowEnd,
                        b.CreatedAt,
                        b.UpdatedAt,
                        b.IsActive
                    ))
                    .ToListAsync();

                var result = new PaginatedResult<BuildingDto>(items, totalCount);
                return ApiResponse<PaginatedResult<BuildingDto>>.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse<PaginatedResult<BuildingDto>>.Fail(ApiResponse.SM40_SYSTEM_ERROR);
            }
        }

        public async Task<ApiResponse<BuildingDto>> GetByIdAsync(string id)
        {
            try
            {
                var dto = await _context.Buildings
                    .AsNoTracking()
                    .Where(b => b.BuildingId == id)
                    .Select(b => new BuildingDto(
                        b.BuildingId,
                        b.ProjectId,
                        b.BuildingCode,
                        b.Name,
                        b.Apartments.Count(),
                        b.Apartments.SelectMany(a => a.ApartmentMembers).Count(),
                        b.TotalFloors,
                        b.TotalBasements,
                        b.TotalArea,
                        b.HandoverDate,
                        (b.HandoverDate.HasValue && b.HandoverDate.Value.AddYears(5) >= DateOnly.FromDateTime(DateTime.Now))
                                ? "Còn bảo hành" : "Hết bảo hành",
                        b.ReceptionPhone,
                        b.Description,
                        b.ReadingWindowStart,
                        b.ReadingWindowEnd,
                        b.CreatedAt,
                        b.UpdatedAt,
                        b.IsActive
                    ))
                    .FirstOrDefaultAsync();

                if (dto == null) return ApiResponse<BuildingDto>.Fail(ApiResponse.SM01_NO_RESULTS);

                return ApiResponse<BuildingDto>.Success(dto);
            }
            catch (Exception)
            {
                return ApiResponse<BuildingDto>.Fail(ApiResponse.SM40_SYSTEM_ERROR);
            }
        }

        // --- CREATE ---
        public async Task<ApiResponse<BuildingDto>> CreateAsync(BuildingCreateDto dto)
        {
            var project = await _projectRepository.FirstOrDefaultAsync(p => p.ProjectId == dto.ProjectId);
            if (project == null) return ApiResponse<BuildingDto>.Fail(ApiResponse.SM27_PROJECT_NOT_FOUND);

            var errorMsg = ValidateBuildingLogic(
                dto.BuildingCode,
                dto.Name,
                dto.TotalFloors,
                dto.TotalBasements,
                dto.ReadingWindowStart,
                dto.ReadingWindowEnd,
                dto.HandoverDate,
                dto.TotalArea,
                dto.ReceptionPhone
            );

            if (errorMsg != null)
            {
                return ApiResponse<BuildingDto>.Fail(ApiResponse.SM25_INVALID_INPUT, null, errorMsg);
            }

            var exists = await _repository.FirstOrDefaultAsync(b => b.ProjectId == dto.ProjectId && b.BuildingCode == dto.BuildingCode);
            if (exists != null) return ApiResponse<BuildingDto>.Fail(ApiResponse.SM16_DUPLICATE_CODE, "BuildingCode");

            var now = DateTime.UtcNow;
            var entity = _mapper.Map<Building>(dto);

            entity.IsActive = true;
            entity.CreatedAt = now;
            entity.UpdatedAt = now;

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return await GetByIdAsync(entity.BuildingId);
        }

        // --- UPDATE ---
        public async Task<ApiResponse> UpdateAsync(string id, BuildingUpdateDto dto)
        {
            var entity = await _repository.FirstOrDefaultAsync(b => b.BuildingId == id);
            if (entity == null) return ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS);

            // [FIX VALIDATION ERROR]
            // Xử lý trường hợp dữ liệu cũ trong DB bị sai (ví dụ: ReadingWindowStart = 0)
            // Nếu DTO không gửi lên (null), ta lấy từ Entity.
            // Nếu giá trị từ Entity <= 0, ta gán giá trị mặc định hợp lệ (1) để bypass validation khi chỉ update status.
            int checkStart = dto.ReadingWindowStart ?? (entity.ReadingWindowStart < 1 ? 1 : entity.ReadingWindowStart);
            int checkEnd = dto.ReadingWindowEnd ?? (entity.ReadingWindowEnd < 1 ? 5 : entity.ReadingWindowEnd);
            int? checkFloors = dto.TotalFloors ?? (entity.TotalFloors < 1 ? 1 : entity.TotalFloors);

            // Các trường có thể null hoặc 0
            int? checkBasements = dto.TotalBasements;
            DateOnly? checkHandover = dto.HandoverDate;
            string? checkName = dto.Name;
            double? checkArea = dto.TotalArea;
            string? checkPhone = dto.ReceptionPhone;

            // Validate logic với các giá trị đã chuẩn hóa
            var errorMsg = ValidateBuildingLogic(
                null, // Mã không đổi
                checkName,
                checkFloors,
                checkBasements,
                checkStart,
                checkEnd,
                checkHandover,
                checkArea,
                checkPhone
            );

            if (errorMsg != null)
            {
                return ApiResponse.Fail(ApiResponse.SM25_INVALID_INPUT, null, errorMsg);
            }

            var now = DateTime.UtcNow;

            if (dto.IsActive != null) 
            {
                // === [LOGIC 1] DEACTIVATE: Chuyển từ Active -> Inactive ===
                bool isDeactivating = dto.IsActive.HasValue && dto.IsActive.Value == false && entity.IsActive == true;

                if (isDeactivating)
                {
                    // 1. Vô hiệu hóa Cư dân (User linked to Apartment in THIS Building)
                    // Tìm users có Apartment thuộc tòa nhà này và đang Active
                    var residentUsers = await _context.Users
                        .Where(u => u.Apartment != null && u.Apartment.BuildingId == id && u.Status == "active")
                        .ToListAsync();

                    foreach (var user in residentUsers)
                    {
                        user.Status = "inactive";
                        user.UpdatedAt = now;
                    }

                    // 2. Vô hiệu hóa Phân công nhân viên (StaffBuildingAssignment for THIS Building)
                    var staffAssignments = await _context.StaffBuildingAssignments
                        .Where(sba => sba.BuildingId == id && sba.IsActive == true)
                        .ToListAsync();

                    foreach (var assignment in staffAssignments)
                    {
                        assignment.IsActive = false;
                        assignment.AssignmentEndDate = DateOnly.FromDateTime(now);
                        assignment.UpdatedAt = now;
                    }
                }

                // === [LOGIC 2] ACTIVATE: Chuyển từ Inactive -> Active (MỚI THÊM) ===
                bool isActivating = dto.IsActive.HasValue && dto.IsActive.Value == true && entity.IsActive == false;

                if (isActivating)
                {
                    // 1. Khôi phục Cư dân: Tìm các User thuộc tòa nhà đang 'Inactive' và chuyển về 'Active'
                    var inactiveResidents = await _context.Users
                        .Where(u => u.Apartment != null && u.Apartment.BuildingId == id && u.Status == "inactive")
                        .ToListAsync();

                    foreach (var user in inactiveResidents)
                    {
                        user.Status = "active";
                        user.UpdatedAt = now;
                    }

                    // 2. Phân công nhân viên: KHÔNG TÁC ĐỘNG (Theo yêu cầu)
                    // Nhân viên sẽ cần được phân công lại thủ công nếu cần.
                }

                entity.IsActive = dto.IsActive.Value;
            }

            _mapper.Map(dto, entity);

            entity.UpdatedAt = now;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync(); // Lưu tất cả thay đổi (Building, User, StaffAssignment)

            return ApiResponse.Success(ApiResponse.SM03_UPDATE_SUCCESS);
        }

        public async Task<ApiResponse<IEnumerable<ApartmentDto>>> GetRentedApartmentsByBuildingAsync(string buildingId)
        {
            // Căn hộ được coi là đang sử dụng nếu có trạng thái "Đã Bán" hoặc "Đang Thuê"
            // ApartmentQueryParameters chỉ nhận một status, nên dùng service để filter thêm
            var query = new ApartmentQueryParameters(buildingId, null, null, null, null);
            var all = await _apartmentService.GetAllAsync(query);

            if (!all.Succeeded || all.Data == null)
            {
                return all;
            }

            var filtered = all.Data
                .Where(a => a.Status == "Đã Bán" || a.Status == "Đang Thuê")
                .ToList();

            if (!filtered.Any())
            {
                return ApiResponse<IEnumerable<ApartmentDto>>.Success(
                    new List<ApartmentDto>(),
                    ApiResponse.SM01_NO_RESULTS
                );
            }

            return ApiResponse<IEnumerable<ApartmentDto>>.Success(filtered);
        }

        public async Task<ApiResponse<PaginatedResult<BuildingDto>>> GetByUserBuildingsAsync(string userId, BuildingQueryParameters query)
        {
            try
            {
                var searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm) ? null : query.SearchTerm.Trim().ToLower();
                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                // Lấy các tòa nhà mà userId được gán và assignment còn hiệu lực
                var queryable = _context.Buildings
                    .AsNoTracking()
                    .Where(b => b.StaffBuildingAssignments.Any(sba =>
                        sba.UserId == userId &&
                        sba.IsActive &&
                        (sba.AssignmentEndDate == null || sba.AssignmentEndDate >= today)
                    ));

                if (searchTerm != null)
                {
                    queryable = queryable.Where(b =>
                        (b.Name != null && b.Name.ToLower().Contains(searchTerm)) ||
                        (b.BuildingCode != null && b.BuildingCode.ToLower().Contains(searchTerm))
                    );
                }

                var totalCount = await queryable.CountAsync();

                var items = await queryable
                    .OrderBy(b => b.BuildingCode)
                    .Skip(query.Skip)
                    .Take(query.Take)
                    .Select(b => new BuildingDto(
                        b.BuildingId,
                        b.ProjectId,
                        b.BuildingCode,
                        b.Name,
                        b.Apartments.Count(),
                        b.Apartments.SelectMany(a => a.ApartmentMembers).Count(),
                        b.TotalFloors,
                        b.TotalBasements,
                        b.TotalArea,
                        b.HandoverDate,
                        (b.HandoverDate.HasValue && b.HandoverDate.Value.AddYears(5) >= DateOnly.FromDateTime(DateTime.Now))
                            ? "Còn bảo hành" : "Hết bảo hành",
                        b.ReceptionPhone,
                        b.Description,
                        b.ReadingWindowStart,
                        b.ReadingWindowEnd,
                        b.CreatedAt,
                        b.UpdatedAt,
                        b.IsActive
                    ))
                    .ToListAsync();

                var result = new PaginatedResult<BuildingDto>(items, totalCount);
                return ApiResponse<PaginatedResult<BuildingDto>>.Success(result);
            }
            catch (Exception)
            {
                return ApiResponse<PaginatedResult<BuildingDto>>.Fail(ApiResponse.SM40_SYSTEM_ERROR);
            }
        }

    }
}