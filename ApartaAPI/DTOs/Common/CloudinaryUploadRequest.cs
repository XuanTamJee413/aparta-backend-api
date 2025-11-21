using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.Common
{
    public class CloudinaryUploadRequest
    {
        [Required(ErrorMessage = ApiResponse.SM02_REQUIRED)]
        public IFormFile? File { get; set; }

        public string? Folder { get; set; }
    }
}

