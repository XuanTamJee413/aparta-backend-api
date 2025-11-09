using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.Common
{
    public class EnumOptionDto
    {
        /// <summary>
        /// Giá trị (ví dụ: "PER_AREA")
        /// </summary>
        [Required]
        public string Value { get; set; } = null!;

        /// <summary>
        /// Tên hiển thị ngắn (ví dụ: "Tính theo diện tích (m2)")
        /// </summary>
        [Required]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Mô tả chi tiết (ví dụ: "Tính phí dựa trên tổng diện tích (m2)...")
        /// </summary>
        public string Description { get; set; } = null!;
    }
}
