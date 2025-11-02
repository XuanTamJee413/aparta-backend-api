using ApartaAPI.Utils.Enums;
using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.PriceQuotations
{
    // hien thi price quuotation list da join building code
    public class PriceQuotationDto
    {
        public string PriceQuotationId { get; set; } = null!;
        public string BuildingId { get; set; } = null!;

        // join Building
        public string BuildingCode { get; set; } = null!;

        public string FeeType { get; set; } = null!;
        public string CalculationMethod { get; set; } = null!;
        public decimal UnitPrice { get; set; }
        public string? Unit { get; set; }
        public string? Note { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatetedAt { get; set; }

    }

    // them moi mot price quotation da loai bo id va create at vi hai cai nay da~ duoc tao tu dong
    public class PriceQuotationCreateDto
    {
        [Required]
        public string BuildingId { get; set; } = null!;

        [Required]
        public string FeeType { get; set; } = null!;

        [Required]
        public ECalculationMethod CalculationMethod { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        public string? Unit { get; set; }

        public string? Note { get; set; }
    }
}
