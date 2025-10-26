using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Subscriptions;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlTypes;
using System.Linq.Expressions;

namespace ApartaAPI.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private static readonly DateTime SqlMinDateTime = (DateTime)SqlDateTime.MinValue;
        private readonly IRepository<Subscription> _repository;
        private readonly IRepository<Project> _projectRepository; // Thêm Repo Project để lấy ngày hết hạn cũ
        private readonly IMapper _mapper;

        // Cập nhật constructor
        public SubscriptionService(
            IRepository<Subscription> repository,
            IRepository<Project> projectRepository,
            IMapper mapper)
        {
            _repository = repository;
            _projectRepository = projectRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// (UC 2.1.1) Lấy danh sách Subscriptions
        /// </summary>
        public async Task<ApiResponse<PaginatedResult<SubscriptionDto>>> GetAllAsync(SubscriptionQueryParameters query)
        {
            try
            {
                var statusFilter = string.IsNullOrWhiteSpace(query.Status)
                    ? null
                    : query.Status.Trim().ToLowerInvariant();

                var createdAtEndInclusive = query.CreatedAtEnd?.Date.AddDays(1).AddTicks(-1);

                // 1. Xây dựng predicate
                Expression<Func<Subscription, bool>> predicate = s =>
                    (statusFilter == null || s.Status.ToLower() == statusFilter) && // Lọc theo Status
                    (!query.CreatedAtStart.HasValue || (s.CreatedAt.HasValue && s.CreatedAt.Value.Date >= query.CreatedAtStart.Value.Date)) && // Lọc theo ngày tạo (từ)
                    (!createdAtEndInclusive.HasValue || (s.CreatedAt.HasValue && s.CreatedAt.Value <= createdAtEndInclusive.Value)); // Lọc theo ngày tạo (đến)

                // 2. Lấy dữ liệu (đã lọc)
                var allEntities = await _repository.FindAsync(predicate);

                // 3. Phân trang và sắp xếp
                var totalCount = allEntities.Count();
                IOrderedEnumerable<Subscription> orderedEntities;

                // Sắp xếp: Nháp mới nhất trước, còn lại thì theo ngày hết hạn (mới -> cũ)
                if (statusFilter == "draft")
                {
                    orderedEntities = allEntities.OrderByDescending(s => s.CreatedAt); // Nháp mới nhất
                }
                else
                {
                    orderedEntities = allEntities.OrderByDescending(s => s.ExpiredAt); // Gói hết hạn gần nhất
                }

                var paginatedEntities = orderedEntities
                                        .Skip(query.Skip)
                                        .Take(query.Take)
                                        .ToList();

                var dtos = _mapper.Map<IEnumerable<SubscriptionDto>>(paginatedEntities);
                var paginatedResult = new PaginatedResult<SubscriptionDto>(dtos, totalCount);

                // (UC 2.1.1 - Alt 3A)
                if (totalCount == 0)
                {
                    return ApiResponse<PaginatedResult<SubscriptionDto>>.Success(paginatedResult, "SM01");
                }

                return ApiResponse<PaginatedResult<SubscriptionDto>>.Success(paginatedResult);
            }
            catch (Exception)
            {
                // Should log the exception
                return ApiResponse<PaginatedResult<SubscriptionDto>>.Fail("An unexpected error occurred while fetching subscriptions.");
            }
        }

        /// <summary>
        /// Lấy Subscription bằng ID
        /// </summary>
        public async Task<ApiResponse<SubscriptionDto>> GetByIdAsync(string id)
        {
            try
            {
                var entity = await _repository.FirstOrDefaultAsync(s => s.SubscriptionId == id);
                if (entity == null)
                {
                    return ApiResponse<SubscriptionDto>.Fail("SM01"); // Not found
                }
                var dto = _mapper.Map<SubscriptionDto>(entity);
                return ApiResponse<SubscriptionDto>.Success(dto);
            }
            catch (Exception)
            {
                return ApiResponse<SubscriptionDto>.Fail("An unexpected error occurred.");
            }
        }

        private async Task<bool> IsSubscriptionTimeOverlapped(string projectId, string? currentSubscriptionId = null)
        {
            // Kiểm tra xem có bất kỳ gói Active nào KHÔNG hết hạn tính đến thời điểm hiện tại không.
            // Nếu có, tức là vẫn đang trong thời hạn gói, không được tạo gói mới.
            var now = DateTime.UtcNow;

            Expression<Func<Subscription, bool>> checkOverlap = s =>
                // Chỉ kiểm tra các gói Active của Project hiện tại
                s.ProjectId == projectId &&
                s.Status == "Active" &&
                // Loại bỏ chính bản ghi đang được cập nhật (trong trường hợp UpdateAsync)
                (currentSubscriptionId == null || s.SubscriptionId != currentSubscriptionId) &&
                // Kiểm tra nếu ngày hết hạn của gói Active còn sau thời điểm hiện tại (tức là còn hạn)
                s.ExpiredAt > now;

            var isOverlapped = await _repository.FirstOrDefaultAsync(checkOverlap);

            // Nếu tìm thấy bất kỳ gói nào còn hạn (ExpiredAt > now), trả về true (bị chồng chéo)
            return isOverlapped != null;
        }
        // ----------------------------------------------


        /// <summary>
        /// (UC 2.1.2) Tạo mới bản ghi gia hạn
        /// </summary>
        public async Task<ApiResponse<SubscriptionDto>> CreateAsync(SubscriptionCreateOrUpdateDto dto)
        {
            try
            {
                // --- Validation ---
                var project = await _projectRepository.FirstOrDefaultAsync(p => p.ProjectId == dto.ProjectId);
                if (project == null)
                {
                    return ApiResponse<SubscriptionDto>.Fail("Project not found.");
                }

                // KIỂM TRA DUPLICATE SUBCRIPTION_CODE (BR-12)
                var exists = await _repository.FirstOrDefaultAsync(s => s.SubscriptionCode == dto.SubscriptionCode);
                if (exists != null)
                {
                    return ApiResponse<SubscriptionDto>.Fail("SM16"); // Duplicate code
                }

                // --- Logic Chống Chồng Chéo (Quan trọng) ---
                if (dto.IsApproved)
                {
                    if (await IsSubscriptionTimeOverlapped(dto.ProjectId))
                    {
                        return ApiResponse<SubscriptionDto>.Fail("SM17 - Project currently has an active subscription. Cannot create a new one until expiration.");
                    }
                }
                // ----------------------------------------------

                var now = DateTime.UtcNow;
                var entity = _mapper.Map<Subscription>(dto);
                entity.CreatedAt = now;
                entity.UpdatedAt = now;

                if (dto.IsApproved)
                {
                    // Tính ngày hết hạn mới: NOW + NumMonths
                    entity.ExpiredAt = now.AddMonths(dto.NumMonths);
                    entity.Status = "Active";
                    project.IsActive = true;
                    await _projectRepository.UpdateAsync(project);
                }
                else
                {
                    entity.Status = "Draft";
                    entity.ExpiredAt = SqlMinDateTime;
                }

                // Cập nhật thông tin thanh toán
                entity.AmountPaid = dto.AmountPaid;
                entity.PaymentMethod = dto.PaymentMethod;
                entity.PaymentDate = dto.PaymentDate;
                entity.PaymentNote = dto.PaymentNote;
                entity.Tax = entity.Tax ?? 0;
                entity.Discount = entity.Discount ?? 0;

                await _repository.AddAsync(entity);
                await _repository.SaveChangesAsync();

                var resultDto = _mapper.Map<SubscriptionDto>(entity);
                return ApiResponse<SubscriptionDto>.Success(resultDto, dto.IsApproved ? "SM10" : "SM04");
            }
            catch (DbUpdateException dbEx)
            {
                if (dbEx.InnerException?.Message.Contains("UNIQUE KEY constraint") ?? false)
                {
                    return ApiResponse<SubscriptionDto>.Fail("SM16");
                }
                return ApiResponse<SubscriptionDto>.Fail("SM15");
            }
            catch (Exception)
            {
                return ApiResponse<SubscriptionDto>.Fail("SM15");
            }
        }

        /// <summary>
        /// (UC 2.1.3) Cập nhật bản nháp gia hạn
        /// </summary>
        public async Task<ApiResponse<SubscriptionDto>> UpdateAsync(string id, SubscriptionCreateOrUpdateDto dto)
        {
            try
            {
                var entity = await _repository.FirstOrDefaultAsync(s => s.SubscriptionId == id);
                if (entity == null)
                {
                    return ApiResponse<SubscriptionDto>.Fail("SM01"); // Not found
                }

                // (UC 2.1.3 - Ex 2E) Chỉ cho phép sửa Draft
                if (entity.Status.ToLowerInvariant() != "draft")
                {
                    return ApiResponse<SubscriptionDto>.Fail("Only draft subscriptions can be updated.");
                }

                // --- Validation ---
                var project = await _projectRepository.FirstOrDefaultAsync(p => p.ProjectId == entity.ProjectId);
                if (project == null)
                {
                    return ApiResponse<SubscriptionDto>.Fail("Associated Project not found.");
                }

                // --- Logic Chống Chồng Chéo (Quan trọng) ---
                if (dto.IsApproved)
                {
                    // Kiểm tra nếu có gói nào khác (ngoại trừ bản thân gói này) còn hạn
                    if (await IsSubscriptionTimeOverlapped(entity.ProjectId, id))
                    {
                        return ApiResponse<SubscriptionDto>.Fail("SM17 - Project currently has another active subscription. Cannot approve this until expiration.");
                    }
                }
                // ----------------------------------------------

                var now = DateTime.UtcNow;
                _mapper.Map(dto, entity);
                entity.UpdatedAt = now;

                if (dto.IsApproved)
                {
                    // NGÀY BẮT ĐẦU LUÔN LÀ NOW
                    entity.ExpiredAt = now.AddMonths(dto.NumMonths);
                    entity.Status = "Active";
                }
                else
                {
                    entity.Status = "Draft";
                    entity.ExpiredAt = SqlMinDateTime;
                }

                // Cập nhật thông tin thanh toán và các trường khác
                entity.AmountPaid = dto.AmountPaid;
                entity.PaymentMethod = dto.PaymentMethod;
                entity.PaymentDate = dto.PaymentDate;
                entity.PaymentNote = dto.PaymentNote;
                entity.Tax = entity.Tax ?? 0;
                entity.Discount = entity.Discount ?? 0;

                await _repository.UpdateAsync(entity);
                await _repository.SaveChangesAsync();

                var resultDto = _mapper.Map<SubscriptionDto>(entity);
                return ApiResponse<SubscriptionDto>.Success(resultDto, dto.IsApproved ? "SM10" : "SM03");
            }
            catch (DbUpdateException dbEx)
            {
                if (dbEx.InnerException?.Message.Contains("UNIQUE KEY constraint") ?? false)
                {
                    return ApiResponse<SubscriptionDto>.Fail("SM16");
                }
                return ApiResponse<SubscriptionDto>.Fail("SM15");
            }
            catch (Exception)
            {
                return ApiResponse<SubscriptionDto>.Fail("SM15");
            }
        }

        /// <summary>
        /// (UC 2.1.4) Xóa bản nháp gia hạn
        /// </summary>
        public async Task<ApiResponse> DeleteDraftAsync(string id)
        {
            try
            {
                var entity = await _repository.FirstOrDefaultAsync(s => s.SubscriptionId == id);
                if (entity == null)
                {
                    return ApiResponse.Fail("SM01"); // Not found
                }

                // (UC 2.1.4 - Ex 2E) Chỉ cho phép xóa Draft
                if (entity.Status.ToLowerInvariant() != "draft")
                {
                    return ApiResponse.Fail("Only draft subscriptions can be deleted.");
                }

                await _repository.RemoveAsync(entity);
                await _repository.SaveChangesAsync(); // BR-10

                return ApiResponse.Success("SM05"); // Delete success
            }
            catch (Exception)
            {
                return ApiResponse.Fail("An unexpected error occurred during deletion."); // Lỗi chung
            }
        }
    }
}