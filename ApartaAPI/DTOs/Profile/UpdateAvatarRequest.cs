using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.Profile
{
    public class UpdateAvatarRequest
    {
        [Required(ErrorMessage = "File ảnh là bắt buộc.")]
        public IFormFile? File { get; set; }
    }
}

