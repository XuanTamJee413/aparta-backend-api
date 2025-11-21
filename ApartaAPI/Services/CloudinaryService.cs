using ApartaAPI.DTOs.Common;
using ApartaAPI.Helpers;
using ApartaAPI.Services.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ApartaAPI.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly string _defaultFolder;

        public CloudinaryService(IOptions<CloudinarySettings> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var settings = options.Value ?? throw new ArgumentNullException(nameof(options.Value));

            if (string.IsNullOrWhiteSpace(settings.CloudName) ||
                string.IsNullOrWhiteSpace(settings.ApiKey) ||
                string.IsNullOrWhiteSpace(settings.ApiSecret))
            {
                throw new ArgumentException("Cloudinary configuration is incomplete.");
            }

            var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
            _cloudinary = new Cloudinary(account);
            _defaultFolder = string.IsNullOrWhiteSpace(settings.DefaultFolder) ? "aparta-library" : settings.DefaultFolder!;
        }

        public async Task<CloudinaryUploadResultDto> UploadImageAsync(IFormFile file, string? folder = null, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File upload is required.", nameof(file));
            }

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = string.IsNullOrWhiteSpace(folder) ? _defaultFolder : folder!.Trim()
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

            if (uploadResult.Error != null)
            {
                throw new InvalidOperationException(uploadResult.Error.Message);
            }

            return new CloudinaryUploadResultDto
            {
                PublicId = uploadResult.PublicId,
                SecureUrl = uploadResult.SecureUrl?.ToString() ?? string.Empty,
                ResourceType = uploadResult.ResourceType,
                Format = uploadResult.Format,
                Bytes = uploadResult.Bytes,
                Version = int.TryParse(uploadResult.Version, out var version) ? version : 0,
                CreatedAt = uploadResult.CreatedAt
            };
        }
    }
}

