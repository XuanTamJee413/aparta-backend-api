using ApartaAPI.DTOs.Buildings;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Subscriptions;
using System.Threading.Tasks;

namespace ApartaAPI.Services.Interfaces
{
    public interface ISubscriptionService
    {
        /// <summary>
        /// (UC 2.1.1) Lấy danh sách Subscriptions (phân trang, lọc theo status, ngày tạo)
        /// </summary>
        Task<ApiResponse<PaginatedResult<SubscriptionDto>>> GetAllAsync(SubscriptionQueryParameters query);

        /// <summary>
        /// Lấy chi tiết Subscription bằng ID (Draft hoặc Approved)
        /// </summary>
        Task<ApiResponse<SubscriptionDto>> GetByIdAsync(string id);

        /// <summary>
        /// (UC 2.1.2) Tạo mới một bản ghi gia hạn (Lưu Nháp hoặc Duyệt)
        /// </summary>
        Task<ApiResponse<SubscriptionDto>> CreateAsync(SubscriptionCreateOrUpdateDto dto);

        /// <summary>
        /// (UC 2.1.3) Cập nhật một bản nháp gia hạn (Lưu Nháp hoặc Duyệt)
        /// </summary>
        Task<ApiResponse<SubscriptionDto>> UpdateAsync(string id, SubscriptionCreateOrUpdateDto dto);

        /// <summary>
        /// (UC 2.1.4) Xóa một bản nháp gia hạn
        /// </summary>
        Task<ApiResponse> DeleteDraftAsync(string id);
    }
}