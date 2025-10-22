using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.Assets
{
    public class AssetDto
    {
        public string AssetId { get; set; }
        public string? Info { get; set; }
        public string? BuildingId { get; set; }
        public int? Quantity { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class AssetCreateDto
    {
        [Required(ErrorMessage = "Thông tin tài sản là bắt buộc.")]
        [StringLength(200, ErrorMessage = "Thông tin không được vượt quá 200 ký tự.")]
        public string Info { get; set; }

        [Required(ErrorMessage = "ID tòa nhà là bắt buộc.")]
        public string BuildingId { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải là một số nguyên dương.")]
        public int Quantity { get; set; }
    }

    public class AssetUpdateDto
    {
        [StringLength(200, ErrorMessage = "Thông tin không được vượt quá 200 ký tự.")]
        public string? Info { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng phải là một số nguyên dương")]
        public int? Quantity { get; set; }
    }
}
