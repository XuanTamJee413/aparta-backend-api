using ApartaAPI.DTOs.Common;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ApartaAPI.Services.Interfaces
{
    public interface ICloudinaryService
    {
        Task<CloudinaryUploadResultDto> UploadImageAsync(IFormFile file, string? folder = null, CancellationToken cancellationToken = default);
    }
}

