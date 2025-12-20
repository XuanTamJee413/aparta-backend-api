using ApartaAPI.DTOs.Common;
using ApartaAPI.Utils.Enums;
using System.Reflection;
using System.Runtime.Serialization;

namespace ApartaAPI.Utils.Helper
{
    public static class EnumHelper
    {
        /// <summary>
        /// Hàm Helper chung (generic) để đọc metadata từ bất kỳ Enum nào.
        /// Nó sẽ tự động tìm [EnumMetadata] và [EnumMember].
        /// </summary>
        private static List<EnumOptionDto> GetEnumOptions<T>() where T : Enum
        {
            var enumType = typeof(T);
            var options = new List<EnumOptionDto>();

            // Duyệt qua tất cả các giá trị (ví dụ: ECalculationMethod.PER_AREA)
            foreach (var value in Enum.GetValues(enumType))
            {
                var member = enumType.GetMember(value.ToString()).FirstOrDefault();
                if (member == null) continue;

                // 1. Lấy Metadata (Name, Description) từ [EnumMetadataAttribute]
                var metadata = member.GetCustomAttribute<EnumMetadataAttribute>();

                // 2. Lấy Value (string) từ [EnumMemberAttribute]
                var enumMember = member.GetCustomAttribute<EnumMemberAttribute>();

                options.Add(new EnumOptionDto
                {
                    Value = enumMember?.Value ?? value.ToString(),
                    Name = metadata?.Name ?? value.ToString(),
                    Description = metadata?.Description ?? string.Empty
                });
            }
            return options;
        }

        /// <summary>
        /// Hàm chính: Lấy danh sách các lựa chọn phương thức tính.
        /// </summary>
        public static List<EnumOptionDto> GetCalculationMethodOptions()
        {
            // Chỉ cần gọi hàm chung và chỉ định Enum ECalculationMethod
            return GetEnumOptions<ECalculationMethod>();
        }
        public static List<EnumOptionDto> GetProposalTypeOptions()
        {
            return GetEnumOptions<EProposalType>();
        }
    }
}
