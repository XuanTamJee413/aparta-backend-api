using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace ApartaAPI.Utils.Enums
{
    public enum EVisitorStatus
    {
        [Display(Name = "Chờ check-in")]
        [EnumMember(Value = "Pending")]
        Pending,

        [Display(Name = "Đã check-in")]
        [EnumMember(Value = "Checked-in")] 
        CheckedIn,

        [Display(Name = "Đã check-out")]
        [EnumMember(Value = "Checked-out")] 
        CheckedOut,
        [Display(Name = "Đã Hủy")]
        [EnumMember(Value = "Canceled")] 
        Canceled
    }
}
