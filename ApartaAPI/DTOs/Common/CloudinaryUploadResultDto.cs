using System;

namespace ApartaAPI.DTOs.Common
{
    public class CloudinaryUploadResultDto
    {
        public string PublicId { get; set; } = string.Empty;
        public string SecureUrl { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public long Bytes { get; set; }
        public int Version { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}

