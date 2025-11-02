using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization; 
using System.Text.Json.Serialization; 

namespace ApartaAPI.Utils.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ECalculationMethod
    {
        [Display(Name = "Tính theo diện tích (m2)")]
        [EnumMember(Value = "PER_AREA")]
        PER_AREA,

        [Display(Name = "Cố định theo phương tiện")]
        [EnumMember(Value = "FIXED_PER_VEHICLE")]
        FIXED_PER_VEHICLE,

        [Display(Name = "Tính theo giờ")]
        [EnumMember(Value = "PER_HOUR")]
        PER_HOUR,

        [Display(Name = "Tính lũy tiến (theo bậc): ví dụ tiền điện, nước,... dùng càng nhiều giá sẽ càng tăng")]
        [EnumMember(Value = "TIERED")]
        TIERED,

        [Display(Name = "Tính đồng giá (ví dụ: nước nóng, gas,..  được cung cấp bởi tòa nhà, vẫn tính theo số như số điện nhưng không lũy tiến)")]
        [EnumMember(Value = "FIXED_RATE")]
        FIXED_RATE,

        [Display(Name = "Tính theo người/tháng")]
        [EnumMember(Value = "PER_PERSON_PER_MONTH")]
        PER_PERSON_PER_MONTH,

        [Display(Name = "Tính theo lượt sử dụng")]
        [EnumMember(Value = "PER_USE")]
        PER_USE,

        [Display(Name = "Thu phí một lần")]
        [EnumMember(Value = "FIXED_ONE_TIME")]
        FIXED_ONE_TIME,

        [Display(Name = "Cố định theo thú cưng/tháng")]
        [EnumMember(Value = "FIXED_PER_PET_PER_MONTH")]
        FIXED_PER_PET_PER_MONTH,

        [Display(Name = "Tính theo gói (ví dụ: thuê BBQ)")]
        [EnumMember(Value = "PER_SLOT")]
        PER_SLOT,

        [Display(Name = "Cố định theo tháng (chung)")]
        [EnumMember(Value = "FIXED_PER_MONTH")]
        FIXED_PER_MONTH,

        [Display(Name = "Tính theo kg")]
        [EnumMember(Value = "PER_KG")]
        PER_KG,

        [Display(Name = "Phạt % theo ngày quá hạn")]
        [EnumMember(Value = "PERCENT_PER_DAY_ON_DEBT")]
        PERCENT_PER_DAY_ON_DEBT
    }
}