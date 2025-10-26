using System;
using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.News
{
    public sealed record NewsSearchDto(
        string? SearchTerm, 
        string? Status     
    );

    public class NewsDto
    {
        public string? NewsId { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? AuthorUserId { get; set; }
        public string? AuthorName { get; set; }
        public string? Status { get; set; } 
        public DateTime? PublishedDate { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public sealed record CreateNewsDto
    {
        [Required(ErrorMessage = "Title là bắt buộc")]
        [StringLength(255, ErrorMessage = "Title không được vượt quá 255 ký tự")]
        public string Title { get; init; } = null!;

        [Required(ErrorMessage = "Content là bắt buộc")]
        public string Content { get; init; } = null!;
    }

    public sealed record UpdateNewsDto
    {
        [StringLength(255, ErrorMessage = "Title không được vượt quá 255 ký tự")]
        public string? Title { get; init; }

        public string? Content { get; init; }

        public string? Status { get; init; } 

        public DateTime? PublishedDate { get; init; }
    }
}

