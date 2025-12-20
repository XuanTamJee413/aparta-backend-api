using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace ApartaAPI.Utils.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EProposalType
    {
        [EnumMetadata("Sửa chữa", "Yêu cầu sửa chữa các thiết bị hỏng hóc trong căn hộ hoặc khu vực chung.")]
        [EnumMember(Value = "REPAIR")]
        REPAIR,

        [EnumMetadata("Khiếu nại", "Phản ánh về chất lượng dịch vụ hoặc thái độ nhân viên.")]
        [EnumMember(Value = "COMPLAINT")]
        COMPLAINT,

        [EnumMetadata("Góp ý", "Đóng góp ý kiến để cải thiện chất lượng sống tại chung cư.")]
        [EnumMember(Value = "SUGGESTION")]
        SUGGESTION,

        [EnumMetadata("Vệ sinh", "Báo cáo các vấn đề liên quan đến vệ sinh môi trường.")]
        [EnumMember(Value = "CLEANING")]
        CLEANING,

        [EnumMetadata("An ninh", "Báo cáo các vấn đề liên quan đến an toàn, trật tự.")]
        [EnumMember(Value = "SECURITY")]
        SECURITY,

        [EnumMetadata("Khác", "Các yêu cầu không thuộc các mục trên.")]
        [EnumMember(Value = "OTHERS")]
        OTHERS
    }
}