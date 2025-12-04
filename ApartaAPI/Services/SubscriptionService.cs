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
        private readonly ApartaDbContext _context;

        public SubscriptionService(
            IRepository<Subscription> repository,
            IRepository<Project> projectRepository,
            IProjectService projectService,
            IMapper mapper,
            ApartaDbContext context)
        {
            _repository = repository;
            _projectRepository = projectRepository;
            _projectService = projectService;
            _mapper = mapper;
            _context = context;
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

                var dateType = string.IsNullOrWhiteSpace(query.DateType)
                    ? "created"
                    : query.DateType.Trim().ToLowerInvariant();

                // [LOGIC MỚI 1]: Nếu đang lọc riêng "Draft", bắt buộc dùng ngày tạo.
                // Vì Draft không có ngày bắt đầu/hết hạn có ý nghĩa.
                if (statusFilter == "draft")
                {
                    dateType = "created";
                }

                var toDateInclusive = query.ToDate?.Date.AddDays(1).AddTicks(-1);

                var queryable = _context.Subscriptions
                    .Include(s => s.Project)
                    .AsNoTracking();

                // 1. Áp dụng Status Filter
                if (statusFilter != null)
                {
                    if (statusFilter == "all")
                    {
                        // Nếu chọn "All" -> Lấy tất cả trạng thái NGOẠI TRỪ 'Draft'
                        queryable = queryable.Where(s => s.Status != "Draft");
                    }
                    else
                    {
                        // Lọc chính xác (Active, Expired, Cancelled, hoặc Draft)
                        queryable = queryable.Where(s => s.Status.ToLower() == statusFilter);
                    }
                }

                // 2. Áp dụng Date Filter
                if (query.FromDate.HasValue || toDateInclusive.HasValue)
                {
                    switch (dateType)
                    {
                        case "payment": // Ngày thanh toán
                                        // Chỉ lấy những thằng đã có ngày thanh toán
                            queryable = queryable.Where(s => s.PaymentDate.HasValue);

                            if (query.FromDate.HasValue)
                                queryable = queryable.Where(s => s.PaymentDate >= query.FromDate.Value);
                            if (toDateInclusive.HasValue)
                                queryable = queryable.Where(s => s.PaymentDate <= toDateInclusive.Value);
                            break;

                        case "expired": // Ngày hết hạn
                                        // [LOGIC MỚI 2]: Tự động loại bỏ Draft
                            queryable = queryable.Where(s => s.Status != "Draft");

                            if (query.FromDate.HasValue)
                                queryable = queryable.Where(s => s.ExpiredAt >= query.FromDate.Value);
                            if (toDateInclusive.HasValue)
                                queryable = queryable.Where(s => s.ExpiredAt <= toDateInclusive.Value);
                            break;

                        case "start": // Ngày bắt đầu (ExpiredAt - NumMonths)
                                      // [LOGIC MỚI 2]: Tự động loại bỏ Draft -> Chặn lỗi SQL Overflow 100%
                            queryable = queryable.Where(s => s.Status != "Draft");

                            if (query.FromDate.HasValue)
                                queryable = queryable.Where(s => s.ExpiredAt.AddMonths(-s.NumMonths) >= query.FromDate.Value);
                            if (toDateInclusive.HasValue)
                                queryable = queryable.Where(s => s.ExpiredAt.AddMonths(-s.NumMonths) <= toDateInclusive.Value);
                            break;

                        case "created": // Ngày tạo
                        default:
                            if (query.FromDate.HasValue)
                                queryable = queryable.Where(s => s.CreatedAt >= query.FromDate.Value);
                            if (toDateInclusive.HasValue)
                                queryable = queryable.Where(s => s.CreatedAt <= toDateInclusive.Value);
                            break;
                    }
                }

                var totalCount = await queryable.CountAsync();

                if (totalCount == 0)
                {
                    return ApiResponse<PaginatedResult<SubscriptionDto>>.Success(
                        new PaginatedResult<SubscriptionDto>(new List<SubscriptionDto>(), 0),
                        ApiResponse.SM01_NO_RESULTS
                    );
                }

                // Sorting
                if (statusFilter == "draft" || dateType == "created")
                {
                    queryable = queryable.OrderByDescending(s => s.CreatedAt);
                }
                else if (dateType == "payment")
                {
                    queryable = queryable.OrderByDescending(s => s.PaymentDate);
                }
                else // expired hoặc start
                {
                    queryable = queryable.OrderByDescending(s => s.ExpiredAt);
                }

                var entities = await queryable
                    .Skip(query.Skip)
                    .Take(query.Take)
                    .ToListAsync();

                var dtos = _mapper.Map<IEnumerable<SubscriptionDto>>(entities);
                var paginatedResult = new PaginatedResult<SubscriptionDto>(dtos, totalCount);

                return ApiResponse<PaginatedResult<SubscriptionDto>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ApiResponse<PaginatedResult<SubscriptionDto>>.Fail(ApiResponse.SM40_SYSTEM_ERROR);
            }
        }

        public async Task<ApiResponse<SubscriptionDto>> GetByIdAsync(string id)
        {
            try
            {
                var entity = await _context.Subscriptions
                    .Include(s => s.Project)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SubscriptionId == id);

                if (entity == null)
                {
                    return ApiResponse<SubscriptionDto>.Fail(ApiResponse.SM01_NO_RESULTS);
                }
                var dto = _mapper.Map<SubscriptionDto>(entity);
                return ApiResponse<SubscriptionDto>.Success(dto);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
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
                    return ApiResponse<SubscriptionDto>.Fail(ApiResponse.SM25_INVALID_INPUT, null, "Chỉ có thể chỉnh sửa các bản nháp.");
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
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
                    return ApiResponse.Fail(ApiResponse.SM21_DELETION_FAILED);
                }

                await _repository.RemoveAsync(entity);
                await _repository.SaveChangesAsync();

                return ApiResponse.SuccessWithCode(ApiResponse.SM05_DELETION_SUCCESS, "Subscription");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ApiResponse.Fail(ApiResponse.SM40_SYSTEM_ERROR);
            }
        }
    }
}