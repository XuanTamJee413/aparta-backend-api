using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization; 
using System.Text.Json.Serialization; 

namespace ApartaAPI.Utils.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ECalculationMethod
    {
        [EnumMetadata("Cố định", "Thu một khoản tiền không đổi hàng tháng (ví dụ: Tiền thuê, Phí quản lý).")]
        [EnumMember(Value = "FIXED")]
        FIXED,

        [EnumMetadata("Theo đồng hồ (đơn giá)", "Tính theo chỉ số tiêu thụ (điện, nước) với 1 giá duy nhất.")]
        [EnumMember(Value = "PER_UNIT_METER")]
        PER_UNIT_METER,

        [EnumMetadata("Theo diện tích (m²)", "Tính phí dựa trên diện tích (m²) của căn hộ.")]
        [EnumMember(Value = "PER_AREA")]
        PER_AREA,

        [EnumMetadata("Theo đầu người", "Tính phí dựa trên số lượng người đăng ký ở trong phòng.")]
        [EnumMember(Value = "PER_PERSON")]
        PER_PERSON,

        [EnumMetadata("Theo số lượng (Item)", "Tính phí dựa trên số lượng vật phẩm đăng ký (ví dụ: Phí giữ xe).")]
        [EnumMember(Value = "PER_ITEM")]
        PER_ITEM,

        [EnumMetadata("Thu một lần", "Chỉ thu một lần duy nhất khi phát sinh (ví dụ: Phí làm thẻ).")]
        [EnumMember(Value = "ONE_TIME")]
        ONE_TIME,

        /*
        // --- Lũy tiến (TIERED) ---
        [EnumMetadata("Tính lũy tiến (Theo bậc)", "Áp dụng đơn giá khác nhau cho các bậc tiêu thụ khác nhau (ví dụ: tiền điện, nước).")]
        [EnumMember(Value = "TIERED")]
        TIERED,
        */
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class EnumMetadataAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public EnumMetadataAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}