using ApartaAPI.Data;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Projects;
using ApartaAPI.DTOs.Subscriptions;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlTypes;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace ApartaAPI.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private static readonly DateTime SqlMinDateTime = (DateTime)SqlDateTime.MinValue;

        private readonly IRepository<Subscription> _repository;
        private readonly IRepository<Project> _projectRepository;
        private readonly IProjectService _projectService; // [Inject] Để gọi Reactivate
        private readonly IMapper _mapper;

        public SubscriptionService(
            IRepository<Subscription> repository,
            IRepository<Project> projectRepository,
            IProjectService projectService,
            IMapper mapper)
        {
            _repository = repository;
            _projectRepository = projectRepository;
            _projectService = projectService;
            _mapper = mapper;
        }

        // --- HELPER VALIDATION ---
        private string? ValidateSubscriptionLogic(
            string? subscriptionCode,
            int? numMonths,
            decimal? amount,
            decimal? amountPaid)
        {
            // 1. Validate Mã Gói
            if (!string.IsNullOrEmpty(subscriptionCode))
            {
                if (!Regex.IsMatch(subscriptionCode, "^[A-Z0-9_-]+$"))
                {
                    return "Mã gói không hợp lệ. Chỉ chấp nhận chữ in hoa (A-Z), số (0-9), gạch ngang (-) và gạch dưới (_).";
                }
                if (subscriptionCode.Length < 3 || subscriptionCode.Length > 50)
                {
                    return "Mã gói phải từ 3 đến 50 ký tự.";
                }
            }

            // 2. Validate Số tháng
            if (numMonths.HasValue && numMonths.Value < 1)
            {
                return "Số tháng đăng ký phải lớn hơn hoặc bằng 1.";
            }

            // 3. Validate Giá tiền
            if (amount.HasValue && amount.Value < 0)
            {
                return "Giá gốc không được là số âm.";
            }

            // 4. Validate Số tiền đã trả
            if (amountPaid.HasValue && amountPaid.Value < 0)
            {
                return "Số tiền đã thanh toán không được là số âm.";
            }

            return null;
        }

        public async Task<ApiResponse<PaginatedResult<SubscriptionDto>>> GetAllAsync(SubscriptionQueryParameters query)
        {
            try
            {
                var statusFilter = string.IsNullOrWhiteSpace(query.Status)
                    ? null
                    : query.Status.Trim().ToLowerInvariant();

                var createdAtEndInclusive = query.CreatedAtEnd?.Date.AddDays(1).AddTicks(-1);

                Expression<Func<Subscription, bool>> predicate = s =>
                    (statusFilter == null || s.Status.ToLower() == statusFilter) &&
                    (!query.CreatedAtStart.HasValue || (s.CreatedAt.HasValue && s.CreatedAt.Value.Date >= query.CreatedAtStart.Value.Date)) &&
                    (!createdAtEndInclusive.HasValue || (s.CreatedAt.HasValue && s.CreatedAt.Value <= createdAtEndInclusive.Value));

                var allEntities = await _repository.FindAsync(predicate);
                var totalCount = allEntities.Count();

                IOrderedEnumerable<Subscription> orderedEntities;
                if (statusFilter == "draft")
                {
                    orderedEntities = allEntities.OrderByDescending(s => s.CreatedAt);
                }
                else
                {
                    orderedEntities = allEntities.OrderByDescending(s => s.ExpiredAt);
                }

                var paginatedEntities = orderedEntities
                                        .Skip(query.Skip)
                                        .Take(query.Take)
                                        .ToList();

                var dtos = _mapper.Map<IEnumerable<SubscriptionDto>>(paginatedEntities);
                var paginatedResult = new PaginatedResult<SubscriptionDto>(dtos, totalCount);

                if (totalCount == 0)
                {
                    return ApiResponse<PaginatedResult<SubscriptionDto>>.Success(paginatedResult, ApiResponse.SM01_NO_RESULTS);
                }

                return ApiResponse<PaginatedResult<SubscriptionDto>>.Success(paginatedResult);
            }
            catch (Exception)
            {
                return ApiResponse<PaginatedResult<SubscriptionDto>>.Fail("An unexpected error occurred while fetching subscriptions.");
            }
        }

        public async Task<ApiResponse<SubscriptionDto>> GetByIdAsync(string id)
        {
            try
            {
                var entity = await _repository.FirstOrDefaultAsync(s => s.SubscriptionId == id);
                if (entity == null)
                {
                    return ApiResponse<SubscriptionDto>.Fail(ApiResponse.SM01_NO_RESULTS);
                }
                var dto = _mapper.Map<SubscriptionDto>(entity);
                return ApiResponse<SubscriptionDto>.Success(dto);
            }
            catch (Exception)
            {
                return ApiResponse<SubscriptionDto>.Fail(ApiResponse.SM40_SYSTEM_ERROR);
            }
        }

        // --- CREATE ---
        public async Task<ApiResponse<SubscriptionDto>> CreateAsync(SubscriptionCreateOrUpdateDto dto)
        {
            try
            {
                // 1. Validation Logic
                var errorMsg = ValidateSubscriptionLogic(
                    dto.SubscriptionCode,
                    dto.NumMonths,
                    dto.Amount,
                    dto.AmountPaid
                );
                if (errorMsg != null)
                {
                    return ApiResponse<SubscriptionDto>.Fail(ApiResponse.SM25_INVALID_INPUT, null, errorMsg);
                }

                // 2. Check Project Exist
                var project = await _projectRepository.FirstOrDefaultAsync(p => p.ProjectId == dto.ProjectId);
                if (project == null)
                {
                    return ApiResponse<SubscriptionDto>.Fail(ApiResponse.SM27_PROJECT_NOT_FOUND);
                }

                // 3. Check Duplicate Code
                var exists = await _repository.FirstOrDefaultAsync(s => s.SubscriptionCode == dto.SubscriptionCode);
                if (exists != null)
                {
                    return ApiResponse<SubscriptionDto>.Fail(ApiResponse.SM16_DUPLICATE_CODE, "SubscriptionCode");
                }

                var now = DateTime.UtcNow;
                var entity = _mapper.Map<Subscription>(dto);
                entity.CreatedAt = now;
                entity.UpdatedAt = now;

                if (dto.IsApproved)
                {
                    // Tìm mốc thời gian hết hạn xa nhất hiện tại của Project này
                    var activeSub = (await _repository.FindAsync(s =>
                            s.ProjectId == dto.ProjectId &&
                            s.Status == "Active"))
                        .OrderByDescending(s => s.ExpiredAt)
                        .FirstOrDefault();

                    DateTime startDate;

                    if (activeSub != null && activeSub.ExpiredAt > now)
                    {
                        // TRƯỜNG HỢP 1: Còn hạn -> Cộng nối tiếp
                        // Không cần Reactivate vì Project chắc chắn đang Active
                        startDate = activeSub.ExpiredAt;
                    }
                    else
                    {
                        // TRƯỜNG HỢP 2: Đã hết hạn (hoặc chưa mua bao giờ) -> Tính từ bây giờ
                        startDate = now;

                        // Nếu Project đang Inactive -> Phải kích hoạt lại
                        if (!project.IsActive)
                        {
                            await _projectService.UpdateAsync(project.ProjectId, new ProjectUpdateDto(
                                null, // Name
                                null, // Address
                                null, // Ward
                                null, // District
                                null, // City
                                null, // BankName
                                null, // BankAccountNumber
                                null, // BankAccountName
                                null, // PayOSClientId
                                null, // PayOSApiKey
                                null, // PayOSChecksumKey
                                true  // IsActive
                            ));
                            
                            project.IsActive = true;
                        }
                    }

                    // Tính ngày hết hạn mới
                    entity.ExpiredAt = startDate.AddMonths(dto.NumMonths);
                    entity.Status = "Active";
                }
                else // Nếu là "Lưu Nháp"
                {
                    entity.Status = "Draft";
                    entity.ExpiredAt = SqlMinDateTime;
                }

                // Map các trường thanh toán
                entity.AmountPaid = dto.AmountPaid;
                entity.PaymentMethod = dto.PaymentMethod;
                entity.PaymentDate = dto.PaymentDate;
                entity.PaymentNote = dto.PaymentNote;
                entity.Tax = entity.Tax ?? 0;
                entity.Discount = entity.Discount ?? 0;

                await _repository.AddAsync(entity);
                await _repository.SaveChangesAsync();

                var resultDto = _mapper.Map<SubscriptionDto>(entity);
                return ApiResponse<SubscriptionDto>.Success(resultDto, dto.IsApproved ? ApiResponse.SM57_SUBSCRIPTION_EXTENDED : ApiResponse.SM04_CREATE_SUCCESS);
            }
            catch (Exception)
            {
                return ApiResponse<SubscriptionDto>.Fail(ApiResponse.SM40_SYSTEM_ERROR);
            }
        }

        // --- UPDATE ---
        public async Task<ApiResponse<SubscriptionDto>> UpdateAsync(string id, SubscriptionCreateOrUpdateDto dto)
        {
            try
            {
                var entity = await _repository.FirstOrDefaultAsync(s => s.SubscriptionId == id);
                if (entity == null) return ApiResponse<SubscriptionDto>.Fail(ApiResponse.SM01_NO_RESULTS);

                // Chỉ cho phép sửa Draft
                if (entity.Status.ToLowerInvariant() != "draft")
                {
                    return ApiResponse<SubscriptionDto>.Fail("Chỉ có thể chỉnh sửa các bản nháp.");
                }

                // Validation
                var errorMsg = ValidateSubscriptionLogic(
                    null, // Không check code trùng ở đây vì logic bên dưới
                    dto.NumMonths,
                    dto.Amount,
                    dto.AmountPaid
                );
                if (errorMsg != null)
                {
                    return ApiResponse<SubscriptionDto>.Fail(ApiResponse.SM25_INVALID_INPUT, null, errorMsg);
                }

                var project = await _projectRepository.FirstOrDefaultAsync(p => p.ProjectId == entity.ProjectId);
                if (project == null) return ApiResponse<SubscriptionDto>.Fail(ApiResponse.SM27_PROJECT_NOT_FOUND);

                var now = DateTime.UtcNow;
                _mapper.Map(dto, entity);
                entity.UpdatedAt = now;

                if (dto.IsApproved)
                {
                    // === LOGIC NETFLIX (STACKING) ===
                    // Tìm mốc thời gian hết hạn xa nhất (trừ bản ghi hiện tại)
                    var activeSub = (await _repository.FindAsync(s =>
                            s.ProjectId == entity.ProjectId &&
                            s.Status == "Active" &&
                            s.SubscriptionId != id))
                        .OrderByDescending(s => s.ExpiredAt)
                        .FirstOrDefault();

                    DateTime startDate;

                    if (activeSub != null && activeSub.ExpiredAt > now)
                    {
                        // Case A: Cộng nối tiếp
                        startDate = activeSub.ExpiredAt;
                    }
                    else
                    {
                        // Case B: Tính từ bây giờ & Kích hoạt lại
                        startDate = now;

                        if (!project.IsActive)
                        {
                            await _projectService.UpdateAsync(project.ProjectId, new ProjectUpdateDto(
                                null, // Name
                                null, // Address
                                null, // Ward
                                null, // District
                                null, // City
                                null, // BankName
                                null, // BankAccountNumber
                                null, // BankAccountName
                                null, // PayOSClientId
                                null, // PayOSApiKey
                                null, // PayOSChecksumKey
                                true  // IsActive
                            ));
                        }
                    }

                    entity.ExpiredAt = startDate.AddMonths(dto.NumMonths);
                    entity.Status = "Active";
                }
                else
                {
                    entity.Status = "Draft";
                    entity.ExpiredAt = SqlMinDateTime;
                }

                // Map các trường thanh toán
                entity.AmountPaid = dto.AmountPaid;
                entity.PaymentMethod = dto.PaymentMethod;
                entity.PaymentDate = dto.PaymentDate;
                entity.PaymentNote = dto.PaymentNote;
                entity.Tax = entity.Tax ?? 0;
                entity.Discount = entity.Discount ?? 0;

                await _repository.UpdateAsync(entity);
                await _repository.SaveChangesAsync();

                var resultDto = _mapper.Map<SubscriptionDto>(entity);
                return ApiResponse<SubscriptionDto>.Success(resultDto, dto.IsApproved ? ApiResponse.SM57_SUBSCRIPTION_EXTENDED : ApiResponse.SM03_UPDATE_SUCCESS);
            }
            catch (Exception)
            {
                return ApiResponse<SubscriptionDto>.Fail(ApiResponse.SM40_SYSTEM_ERROR);
            }
        }

        public async Task<ApiResponse> DeleteDraftAsync(string id)
        {
            try
            {
                var entity = await _repository.FirstOrDefaultAsync(s => s.SubscriptionId == id);
                if (entity == null) return ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS);

                if (entity.Status.ToLowerInvariant() != "draft")
                {
                    return ApiResponse.Fail("Chỉ có thể xóa các bản nháp.");
                }

                await _repository.RemoveAsync(entity);
                await _repository.SaveChangesAsync();

                return ApiResponse.SuccessWithCode(ApiResponse.SM05_DELETION_SUCCESS, "Subscription");
            }
            catch (Exception)
            {
                return ApiResponse.Fail(ApiResponse.SM40_SYSTEM_ERROR);
            }
        }
    }
}