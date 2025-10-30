using ApartaAPI.DTOs.Common; // Thêm
using ApartaAPI.DTOs.Projects;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq; // Cần cho filter/sort
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ApartaAPI.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IRepository<Project> _repository;
        private readonly IRepository<Subscription> _subscriptionRepository;
        private readonly IMapper _mapper;

        // --- CẬP NHẬT CONSTRUCTOR ---
        public ProjectService(
            IRepository<Project> repository,
            IRepository<Subscription> subscriptionRepository, // Thêm
            IRepository<Building> buildingRepository, // Thêm cho mục 2
            IMapper mapper)
        {
            _repository = repository;
            _subscriptionRepository = subscriptionRepository; // Thêm
            _mapper = mapper;
        }

        // (UC 2.1.3)
        public async Task<ApiResponse<IEnumerable<ProjectDto>>> GetAllAsync(ProjectQueryParameters query)
        {
            var searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm)
                ? null
                : query.SearchTerm.Trim().ToLowerInvariant();

            // 1 & 2: Xây dựng biểu thức (predicate) để lọc phía CSDL
            // Bằng cách này, chúng ta không tải toàn bộ bảng về bộ nhớ.
            Expression<Func<Project, bool>> predicate = p =>
                // Filter theo IsActive
                (!query.IsActive.HasValue || p.IsActive == query.IsActive.Value) &&
                (searchTerm == null ||
                    (p.Name.ToLower().Contains(searchTerm) ||
                     p.ProjectCode.ToLower().Contains(searchTerm)));

            var entities = await _repository.FindAsync(predicate);

            // 3. Sort (Sắp xếp)
            // Vì IRepository không cung cấp phương thức OrderBy,
            // bước sắp xếp này VẪN phải thực hiện in-memory.
            // Nhưng giờ chúng ta chỉ sắp xếp trên dữ liệu đã được lọc, hiệu quả hơn nhiều.
            IOrderedEnumerable<Project> sortedEntities;
            bool isDescending = query.SortOrder?.ToLowerInvariant() == "desc";

            switch (query.SortBy?.ToLowerInvariant())
            {
                case "numapartments":
                    sortedEntities = isDescending
                        ? entities.OrderByDescending(p => p.NumApartments)
                        : entities.OrderBy(p => p.NumApartments);
                    break;
                case "numbuildings":
                    sortedEntities = isDescending
                        ? entities.OrderByDescending(p => p.NumBuildings)
                        : entities.OrderBy(p => p.NumBuildings);
                    break;
                default:
                    // Mặc định sort theo CreatedAt (mới nhất trước)
                    sortedEntities = entities.OrderByDescending(p => p.CreatedAt);
                    break;
            }

            var dtos = _mapper.Map<IEnumerable<ProjectDto>>(sortedEntities);

            // SRS 2.1.3 - 2A: Trả về SM01 khi không có kết quả
            if (!dtos.Any())
            {
                return ApiResponse<IEnumerable<ProjectDto>>.Success(new List<ProjectDto>(), "SM01");
            }

            return ApiResponse<IEnumerable<ProjectDto>>.Success(dtos);
        }

        public async Task<ApiResponse<ProjectDto>> GetByIdAsync(string id)
        {
            var entity = await _repository.FirstOrDefaultAsync(p => p.ProjectId == id);

            if (entity == null)
            {
                return ApiResponse<ProjectDto>.Fail("SM01");
            }

            var dto = _mapper.Map<ProjectDto>(entity);
            return ApiResponse<ProjectDto>.Success(dto);
        }

        // (UC 2.1.4)
        public async Task<ApiResponse<ProjectDto>> CreateAsync(ProjectCreateDto dto)
        {
            // Exception 3E: Required field missing
            if (string.IsNullOrWhiteSpace(dto.ProjectCode) || string.IsNullOrWhiteSpace(dto.Name))
            {
                return ApiResponse<ProjectDto>.Fail("SM02"); //
            }

            // Exception 3E: Duplicate Project Code
            var exists = await _repository.FirstOrDefaultAsync(p => p.ProjectCode == dto.ProjectCode);
            if (exists != null)
            {
                return ApiResponse<ProjectDto>.Fail("SM16"); //
            }

            var now = DateTime.UtcNow;
            var entity = _mapper.Map<Project>(dto);

            entity.CreatedAt ??= now;
            entity.UpdatedAt = now;
            entity.IsActive = true;

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            var resultDto = _mapper.Map<ProjectDto>(entity);
            // Normal Flow: Trả về SM04 (Create success)
            return ApiResponse<ProjectDto>.Success(resultDto, "SM04");
        }

        // (UC 2.1.5)
        public async Task<ApiResponse> UpdateAsync(string id, ProjectUpdateDto dto)
        {
            var entity = await _repository.FirstOrDefaultAsync(p => p.ProjectId == id);
            if (entity == null)
            {
                return ApiResponse.Fail("SM01"); // Not Found
            }

            // --- KIỂM TRA VALIDATION CƠ BẢN ---
            if (dto.Name != null && string.IsNullOrWhiteSpace(dto.Name))
            {
                return ApiResponse.Fail("SM02"); // Name không được rỗng nếu có cập nhật
            }
            // BR-19: ProjectCode không được sửa, bỏ qua check duplicate nếu code không đổi hoặc null

            // --- LOGIC HỦY SUBSCRIPTION KHI PROJECT DEACTIVATE (BR-20) ---
            bool projectDeactivated = dto.IsActive.HasValue && dto.IsActive.Value == false && entity.IsActive == true;

            if (projectDeactivated)
            {
                var now = DateTime.UtcNow;

                // 1. Hủy Subscription Active
                var activeSubscription = await _subscriptionRepository.FirstOrDefaultAsync(
                    s => s.ProjectId == id && s.Status == "Active" && s.ExpiredAt > now
                );

                if (activeSubscription != null)
                {
                    activeSubscription.Status = "Cancelled";
                    activeSubscription.ExpiredAt = now;
                    activeSubscription.UpdatedAt = now;
                    await _subscriptionRepository.UpdateAsync(activeSubscription);
                }
            }
            _mapper.Map(dto, entity);
            entity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);

            bool success = await _repository.SaveChangesAsync();

            if (!success)
            {
                // Có thể thêm log lỗi ở đây
                return ApiResponse.Fail("SM15"); // Lỗi lưu CSDL chung
            }

            // Normal Flow: Trả về SM03 (Update success)
            return ApiResponse.Success("SM03");
        }
    }
}