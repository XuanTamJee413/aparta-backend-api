using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.News;

namespace ApartaAPI.Services.Interfaces
{
    public interface INewsService
    {
        Task<ApiResponse<IEnumerable<NewsDto>>> GetAllNewsAsync(NewsSearchDto query, string currentUserId);
        Task<ApiResponse<NewsDto>> CreateNewsAsync(CreateNewsDto dto, string authorUserId);
        Task<ApiResponse<NewsDto>> UpdateNewsAsync(string newsId, UpdateNewsDto dto, string userId);
        Task<ApiResponse> DeleteNewsAsync(string newsId, string userId);
    }
}

