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

        // --- 1. VALIDATE LOGIC (Bao gồm cả format mã tòa nhà) ---
        private string? ValidateBuildingLogic(
            string? buildingCode,
            string? name, // [MỚI] Thêm tham số tên
            int? floors,
            int? basements,
            int? startDay,
            int? endDay,
            DateOnly? handoverDate)
        {
            // Validate Format Mã tòa nhà
            if (!string.IsNullOrEmpty(buildingCode))
            {
                if (!Regex.IsMatch(buildingCode, "^[A-Z0-9_]+$"))
                {
                    return "Mã tòa nhà không hợp lệ. Chỉ chấp nhận chữ in hoa (A-Z), số (0-9) và gạch dưới (_).";
                }
            }

            // [MỚI] Validate Tên tòa nhà
            if (name != null) // Create thì luôn có, Update thì có thể null (không sửa)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return "Tên tòa nhà không được để trống.";

                var cleanName = name.Trim();
                if (cleanName.Length < 3 || cleanName.Length > 100)
                    return "Tên tòa nhà phải từ 3 đến 100 ký tự.";

                // (Tùy chọn) Chặn ký tự đặc biệt quá mức nếu muốn
                // if (Regex.IsMatch(cleanName, @"[<>]")) return "Tên tòa nhà chứa ký tự không an toàn.";
            }

            // Validate Số tầng
            if (floors.HasValue && (floors < 1 || floors > 200))
                return "Số tầng nổi phải từ 1 đến 200.";

            // Validate Số hầm
            if (basements.HasValue && (basements < 0 || basements > 10))
                return "Số tầng hầm phải từ 0 đến 10.";

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

            // Validate Logic Khoảng ngày
            if (startDay.HasValue && endDay.HasValue && startDay >= endDay)
                return "Ngày bắt đầu chốt số phải nhỏ hơn ngày kết thúc.";

            return null; // Hợp lệ
        }

        public async Task<ApiResponse<PaginatedResult<BuildingDto>>> GetAllAsync(BuildingQueryParameters query)
        {
            try
            {
                var searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm) ? null : query.SearchTerm.Trim().ToLower();
                var queryable = _context.Buildings.AsNoTracking();

                if (searchTerm != null)
                {
                    queryable = queryable.Where(b => b.Name.ToLower().Contains(searchTerm)
                                                  || b.BuildingCode.ToLower().Contains(searchTerm));
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
            // 1. Validate Project
            var project = await _projectRepository.FirstOrDefaultAsync(p => p.ProjectId == dto.ProjectId);
            if (project == null) return ApiResponse<BuildingDto>.Fail(ApiResponse.SM27_PROJECT_NOT_FOUND);

            // 2. Validate Logic (Format Code, Số tầng, Ngày chốt số...)
            var errorMsg = ValidateBuildingLogic(
                dto.BuildingCode,
                dto.Name,
                dto.TotalFloors,
                dto.TotalBasements,
                dto.ReadingWindowStart,
                dto.ReadingWindowEnd,
                dto.HandoverDate
            );
            if (errorMsg != null)
            {
                return ApiResponse<BuildingDto>.Fail(ApiResponse.SM25_INVALID_INPUT, null, errorMsg);
            }

            // 3. Check Duplicate (Dùng chính xác mã FE gửi lên để check)
            var exists = await _repository.FirstOrDefaultAsync(b => b.ProjectId == dto.ProjectId && b.BuildingCode == dto.BuildingCode);
            if (exists != null) return ApiResponse<BuildingDto>.Fail(ApiResponse.SM16_DUPLICATE_CODE, "BuildingCode");

            // 4. Map & Save
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

            // 1. Validate Logic
            int checkStart = dto.ReadingWindowStart ?? entity.ReadingWindowStart;
            int checkEnd = dto.ReadingWindowEnd ?? entity.ReadingWindowEnd;
            int? checkFloors = dto.TotalFloors;
            int? checkBasements = dto.TotalBasements;
            DateOnly? checkHandover = dto.HandoverDate;
            string? checkName = dto.Name;

            // Validate
            var errorMsg = ValidateBuildingLogic(
                null, // Mã không đổi
                checkName,
                checkFloors,
                checkBasements,
                checkStart,
                checkEnd,
                checkHandover);

            if (errorMsg != null)
            {
                return ApiResponse.Fail(ApiResponse.SM25_INVALID_INPUT, null, errorMsg);
            }

            // 2. Map & Save
            _mapper.Map(dto, entity);
            entity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();

            return ApiResponse.Success(ApiResponse.SM03_UPDATE_SUCCESS);
        }

        public async Task<ApiResponse<IEnumerable<ApartmentDto>>> GetRentedApartmentsByBuildingAsync(string buildingId)
        {
            var query = new ApartmentQueryParameters(buildingId, "Đã thuê", null, null, null);
            return await _apartmentService.GetAllAsync(query);
        }
    }
}