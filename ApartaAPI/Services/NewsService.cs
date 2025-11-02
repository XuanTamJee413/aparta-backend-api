using ApartaAPI.Data;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.News;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ApartaAPI.Services
{
    public class NewsService : INewsService
    {
        private readonly IRepository<News> _newsRepository;
        private readonly ApartaDbContext _context;

        public NewsService(IRepository<News> newsRepository, ApartaDbContext context)
        {
            _newsRepository = newsRepository;
            _context = context;
        }

        public async Task<ApiResponse<IEnumerable<NewsDto>>> GetAllNewsAsync(NewsSearchDto query)
        {
            var searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm)
                ? null
                : query.SearchTerm.Trim().ToLowerInvariant();

            var newsQuery = _context.News
                .Include(n => n.AuthorUser)
                .AsQueryable();

            if (searchTerm != null)
            {
                newsQuery = newsQuery.Where(n =>
                    (n.Title != null && n.Title.ToLower().Contains(searchTerm)) ||
                    (n.Content != null && n.Content.ToLower().Contains(searchTerm)));
            }

            // Filter theo status
            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                newsQuery = newsQuery.Where(n => n.Status == query.Status);
            }

            // xeeps theo thời gian
            var newsList = await newsQuery
                .OrderByDescending(n => n.PublishedDate ?? n.CreatedAt)
                .ToListAsync();

            if (!newsList.Any())
            {
                return ApiResponse<IEnumerable<NewsDto>>.Success(
                    new List<NewsDto>(),
                    ApiResponse.SM01_NO_RESULTS
                );
            }

            var newsDtos = newsList.Select(n => new NewsDto
            {
                NewsId = n.NewsId,
                Title = n.Title,
                Content = n.Content,
                AuthorUserId = n.AuthorUserId,
                AuthorName = n.AuthorUser?.Name,
                Status = n.Status,
                PublishedDate = n.PublishedDate,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt
            }).ToList();

            return ApiResponse<IEnumerable<NewsDto>>.Success(newsDtos);
        }

        public async Task<ApiResponse<NewsDto>> CreateNewsAsync(CreateNewsDto dto, string authorUserId)
        {
            var now = DateTime.UtcNow;

            var newNews = new News
            {
                NewsId = Guid.NewGuid().ToString("N"),
                AuthorUserId = authorUserId,
                Title = dto.Title,
                Content = dto.Content,
                Status = "draft",
                PublishedDate = null, 
                CreatedAt = now,
                UpdatedAt = now
            };

            await _newsRepository.AddAsync(newNews);
            await _newsRepository.SaveChangesAsync();

            var author = await _context.Users.FirstOrDefaultAsync(u => u.UserId == authorUserId);

            var resultDto = new NewsDto
            {
                NewsId = newNews.NewsId,
                Title = newNews.Title,
                Content = newNews.Content,
                AuthorUserId = newNews.AuthorUserId,
                AuthorName = author?.Name,
                Status = newNews.Status,
                PublishedDate = newNews.PublishedDate,
                CreatedAt = newNews.CreatedAt,
                UpdatedAt = newNews.UpdatedAt
            };

            return ApiResponse<NewsDto>.Success(resultDto, ApiResponse.SM04_CREATE_SUCCESS);
        }

        public async Task<ApiResponse<NewsDto>> UpdateNewsAsync(string newsId, UpdateNewsDto dto, string userId)
        {
            var news = await _newsRepository.FirstOrDefaultAsync(n => n.NewsId == newsId);
            if (news == null)
            {
                return ApiResponse<NewsDto>.Fail(ApiResponse.SM01_NO_RESULTS); 
            }

            // Kiểm tra quyền: author, admin, hoặc user có permission "news.update"
            var user = await _context.Users
                .Include(u => u.Role)
                    .ThenInclude(r => r.Permissions)
                .FirstOrDefaultAsync(u => u.UserId == userId);
            
            if (user == null)
            {
                return ApiResponse<NewsDto>.Fail("User not found");
            }

            var isAuthor = news.AuthorUserId == userId;
            var roleName = user.Role?.RoleName?.Trim().ToLower() ?? "";
            var isAdmin = roleName == "admin";
            
            // Kiểm tra permission "news.update" trong role
            var hasUpdatePermission = user.Role?.Permissions?
                .Any(p => p.Name?.Trim().ToLower() == "news.update") ?? false;

            if (!isAuthor && !isAdmin && !hasUpdatePermission)
            {
                return ApiResponse<NewsDto>.Fail("You don't have permission to update this news. Only author, admin, or user with 'news.update' permission can update.");
            }

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(dto.Title))
            {
                news.Title = dto.Title;
            }

            if (!string.IsNullOrWhiteSpace(dto.Content))
            {
                news.Content = dto.Content;
            }

            // Update PublishedDate if provided
            if (dto.PublishedDate.HasValue)
            {
                news.PublishedDate = dto.PublishedDate;
            }

            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                // Nếu chuyển từ draft/delete → active, set PublishedDate = now
                if (dto.Status == "active" && news.Status != "active")
                {
                    news.PublishedDate = DateTime.UtcNow;
                }
                
                news.Status = dto.Status;
            }

            news.UpdatedAt = DateTime.UtcNow;

            await _newsRepository.UpdateAsync(news);
            await _newsRepository.SaveChangesAsync();

            // Get author name for response
            var author = await _context.Users.FirstOrDefaultAsync(u => u.UserId == news.AuthorUserId);

            var resultDto = new NewsDto
            {
                NewsId = news.NewsId,
                Title = news.Title,
                Content = news.Content,
                AuthorUserId = news.AuthorUserId,
                AuthorName = author?.Name,
                Status = news.Status,
                PublishedDate = news.PublishedDate,
                CreatedAt = news.CreatedAt,
                UpdatedAt = news.UpdatedAt
            };

            return ApiResponse<NewsDto>.Success(resultDto, ApiResponse.SM03_UPDATE_SUCCESS);
        }

        public async Task<ApiResponse> DeleteNewsAsync(string newsId, string userId)
        {
            var news = await _newsRepository.FirstOrDefaultAsync(n => n.NewsId == newsId);
            if (news == null)
            {
                return ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS);
            }

            // Kiểm tra quyền: author, admin, hoặc user có permission "news.delete"
            var user = await _context.Users
                .Include(u => u.Role)
                    .ThenInclude(r => r.Permissions)
                .FirstOrDefaultAsync(u => u.UserId == userId);
            
            if (user == null)
            {
                return ApiResponse.Fail("User not found");
            }

            var isAuthor = news.AuthorUserId == userId;
            var roleName = user.Role?.RoleName?.Trim().ToLower() ?? "";
            var isAdmin = roleName == "admin";
            
            // Kiểm tra permission "news.delete" trong role
            var hasDeletePermission = user.Role?.Permissions?
                .Any(p => p.Name?.Trim().ToLower() == "news.delete") ?? false;

            if (!isAuthor && !isAdmin && !hasDeletePermission)
            {
                return ApiResponse.Fail("You don't have permission to delete this news. Only author, admin, or user with 'news.delete' permission can delete.");
            }

            // Soft delete: Set Status = "delete"
            news.Status = "delete";
            news.UpdatedAt = DateTime.UtcNow;

            await _newsRepository.UpdateAsync(news);
            await _newsRepository.SaveChangesAsync();

            return ApiResponse.Success(ApiResponse.SM05_DELETION_SUCCESS);
        }
    }
}

