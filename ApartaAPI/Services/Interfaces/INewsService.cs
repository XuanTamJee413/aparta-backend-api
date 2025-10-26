using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.News;

namespace ApartaAPI.Services.Interfaces
{
    public interface INewsService
    {
        Task<ApiResponse<IEnumerable<NewsDto>>> GetAllNewsAsync(NewsSearchDto query);
        Task<ApiResponse<NewsDto>> CreateNewsAsync(CreateNewsDto dto, string authorUserId);
        Task<ApiResponse<NewsDto>> UpdateNewsAsync(string newsId, UpdateNewsDto dto);
        Task<ApiResponse> DeleteNewsAsync(string newsId);
    }
}

