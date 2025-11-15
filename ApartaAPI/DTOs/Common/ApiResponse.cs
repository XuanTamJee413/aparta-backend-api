namespace ApartaAPI.DTOs.Common
{
    public record ApiResponse
    {
        public bool Succeeded { get; init; }
        public string Message { get; init; } = string.Empty;


        // SM01: Không tìm thấy kết quả (dùng chung cho nhiều trường hợp)
        public const string SM01_NO_RESULTS = "Không tìm thấy kết quả phù hợp.";

        // SM02: Trường bắt buộc (dùng chung cho validation)
        public const string SM02_REQUIRED = "Trường này là bắt buộc.";

        // SM03: Cập nhật thành công (dùng chung cho update operations)
        public const string SM03_UPDATE_SUCCESS = "Cập nhật thông tin thành công.";

        // SM04: Tạo mới thành công (dùng chung cho create operations với {objectName})
        public const string SM04_CREATE_SUCCESS = "Tạo mới {objectName} thành công.";

        // SM05: Xóa thành công (dùng chung cho delete operations với {objectName})
        public const string SM05_DELETION_SUCCESS = "Xóa {objectName} thành công.";

        // SM06: Giao nhiệm vụ thành công
        public const string SM06_TASK_ASSIGN_SUCCESS = "Giao nhiệm vụ thành công.";

        // SM07: Đăng nhập thất bại
        public const string SM07_LOGIN_FAIL = "Sai tên đăng nhập hoặc mật khẩu. Vui lòng kiểm tra lại.";

        // SM08: Vượt quá độ dài tối đa (với {max_length})
        public const string SM08_EXCEEDED_LENGTH = "Vượt quá độ dài tối đa {max_length} ký tự.";

        // SM09: Đặt chỗ không hợp lệ
        public const string SM09_INVALID_BOOKING = "Cư dân khác đã đặt chỗ khung giờ này. Vui lòng chọn thời gian khác.";

        // SM10: Thanh toán hóa đơn thành công
        public const string SM10_PAYMENT_SUCCESS = "Thanh toán hóa đơn thành công.";

        // SM11: Ghi chỉ số tiện ích thành công
        public const string SM11_INDEX_RECORD_SUCCESS = "Ghi chỉ số tiện ích thành công.";

        // SM12: Cảnh báo nợ quá hạn
        public const string SM12_DEBT_WARNING = "Căn hộ này có nợ quá hạn. Vui lòng thanh toán để tránh bị hạn chế dịch vụ.";

        // SM13: Không tìm thấy tài khoản
        public const string SM13_ACCOUNT_NOT_FOUND = "Tài khoản không tồn tại. Vui lòng kiểm tra thông tin.";

        // SM14: Khai báo khách/ở tạm thành công
        public const string SM14_DECLARATION_SUCCESS = "Khai báo khách/ở tạm thành công.";

        // SM
        // : Thanh toán thất bại (dùng chung cho lỗi thanh toán và lỗi hệ thống)
        public const string SM15_PAYMENT_FAILED = "Thanh toán thất bại. Vui lòng thử lại hoặc liên hệ hỗ trợ.";

        // SM16: Mã trùng lặp (với {fieldName} - dùng chung cho BuildingCode, ProjectCode, SubscriptionCode, RoleName, Phone, Email, StaffCode)
        public const string SM16_DUPLICATE_CODE = "{fieldName} đã tồn tại. Vui lòng sử dụng giá trị khác.";

        // SM17: Không có quyền thực hiện
        public const string SM17_PERMISSION_DENIED = "Bạn không có quyền thực hiện hành động này.";

        // SM18: Nhập dữ liệu thành công
        public const string SM18_IMPORT_SUCCESS = "Nhập dữ liệu thành công.";

        // SM19: Xuất dữ liệu thành công
        public const string SM19_EXPORT_SUCCESS = "Xuất dữ liệu thành công.";

        // SM20: Không có thay đổi
        public const string SM20_NO_CHANGES = "Không phát hiện thay đổi nào.";

        // SM21: Xóa thất bại (do đang sử dụng)
        public const string SM21_DELETION_FAILED = "Không thể xóa. Mục này đang được sử dụng hoặc có ràng buộc.";

        // SM22: Mã giảm giá không hợp lệ
        public const string SM22_INVALID_COUPON = "Mã giảm giá không tồn tại hoặc không hợp lệ.";

        // SM23: Nhập dữ liệu thất bại
        public const string SM23_IMPORT_FAILED = "Nhập dữ liệu thất bại. Vui lòng kiểm tra định dạng file và dữ liệu.";

        // SM24: Vai trò hệ thống không thể chỉnh sửa
        public const string SM24_SYSTEM_ROLES_IMMUTABLE = "Vai trò hệ thống không thể chỉnh sửa.";

        // SM25: Dữ liệu đầu vào không hợp lệ
        public const string SM25_INVALID_INPUT = "Dữ liệu đầu vào không hợp lệ.";

        // SM26: Tòa nhà không hoạt động
        public const string SM26_BUILDING_NOT_ACTIVE = "Tòa nhà không hoạt động.";

        // SM27: Không tìm thấy dự án
        public const string SM27_PROJECT_NOT_FOUND = "Không tìm thấy dự án.";

        // SM28: Dự án không hoạt động
        public const string SM28_PROJECT_NOT_ACTIVE = "Dự án không hoạt động.";

        // SM29: Không thể xác định người dùng
        public const string SM29_USER_NOT_FOUND = "Không thể xác định người dùng.";

        // SM30: Chỉ số đã bị khóa (đã dùng để xuất hóa đơn)
        public const string SM30_READING_LOCKED = "Không thể sửa. Chỉ số này đã được dùng để xuất hóa đơn.";

        // SM31: Danh sách chỉ số trống
        public const string SM31_READING_LIST_EMPTY = "Danh sách chỉ số không được để trống.";

        // SM32: Dữ liệu cập nhật chỉ số trống
        public const string SM32_READING_UPDATE_EMPTY = "Dữ liệu cập nhật không được để trống.";

        // SM33: Tạo chỉ số thành công (với {count})
        public const string SM33_METER_READING_CREATE_SUCCESS = "Tạo thành công {count} bản ghi chỉ số.";

        // SM34: Đã tồn tại chỉ số trong tháng này
        public const string SM34_READING_EXISTS_IN_PERIOD = "Đã tồn tại chỉ số {feeType} trong tháng {billingPeriod}. Vui lòng cập nhật thay vì tạo mới.";

        // SM35: Chỉ số mới phải lớn hơn hoặc bằng chỉ số tháng trước
        public const string SM35_READING_VALUE_TOO_LOW = "Chỉ số mới ({newValue}) phải lớn hơn hoặc bằng chỉ số tháng trước ({previousValue}).";

        // SM36: Lấy danh sách hóa đơn thành công
        public const string SM36_INVOICE_LIST_SUCCESS = "Lấy danh sách hóa đơn thành công.";

        // SM37: Tạo link thanh toán thành công
        public const string SM37_PAYMENT_LINK_SUCCESS = "Tạo link thanh toán thành công.";

        // SM38: Tạo hóa đơn thành công (với {count})
        public const string SM38_INVOICE_GENERATE_SUCCESS = "Đã xử lý thành công {count} chỉ số.";

        // SM39: Không thể tạo link thanh toán
        public const string SM39_PAYMENT_LINK_FAILED = "Không thể tạo link thanh toán. Vui lòng kiểm tra lại hóa đơn.";

        // SM40: Lỗi hệ thống
        public const string SM40_SYSTEM_ERROR = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.";

        // SM41: Lấy thông tin chi tiết hóa đơn thành công
        public const string SM41_INVOICE_DETAIL_SUCCESS = "Lấy thông tin chi tiết hóa đơn thành công.";

        public static string GetMessageFromCode(string code)
        {
            return code switch
            {
                SM01_NO_RESULTS => SM01_NO_RESULTS,
                SM02_REQUIRED => SM02_REQUIRED,
                SM03_UPDATE_SUCCESS => SM03_UPDATE_SUCCESS,
                SM04_CREATE_SUCCESS => SM04_CREATE_SUCCESS,
                SM05_DELETION_SUCCESS => SM05_DELETION_SUCCESS,
                SM06_TASK_ASSIGN_SUCCESS => SM06_TASK_ASSIGN_SUCCESS,
                SM07_LOGIN_FAIL => SM07_LOGIN_FAIL,
                SM08_EXCEEDED_LENGTH => SM08_EXCEEDED_LENGTH,
                SM09_INVALID_BOOKING => SM09_INVALID_BOOKING,
                SM10_PAYMENT_SUCCESS => SM10_PAYMENT_SUCCESS,
                SM11_INDEX_RECORD_SUCCESS => SM11_INDEX_RECORD_SUCCESS,
                SM12_DEBT_WARNING => SM12_DEBT_WARNING,
                SM13_ACCOUNT_NOT_FOUND => SM13_ACCOUNT_NOT_FOUND,
                SM14_DECLARATION_SUCCESS => SM14_DECLARATION_SUCCESS,
                SM15_PAYMENT_FAILED => SM15_PAYMENT_FAILED,
                SM16_DUPLICATE_CODE => SM16_DUPLICATE_CODE,
                SM17_PERMISSION_DENIED => SM17_PERMISSION_DENIED,
                SM18_IMPORT_SUCCESS => SM18_IMPORT_SUCCESS,
                SM19_EXPORT_SUCCESS => SM19_EXPORT_SUCCESS,
                SM20_NO_CHANGES => SM20_NO_CHANGES,
                SM21_DELETION_FAILED => SM21_DELETION_FAILED,
                SM22_INVALID_COUPON => SM22_INVALID_COUPON,
                SM23_IMPORT_FAILED => SM23_IMPORT_FAILED,
                SM24_SYSTEM_ROLES_IMMUTABLE => SM24_SYSTEM_ROLES_IMMUTABLE,
                SM25_INVALID_INPUT => SM25_INVALID_INPUT,
                SM26_BUILDING_NOT_ACTIVE => SM26_BUILDING_NOT_ACTIVE,
                SM27_PROJECT_NOT_FOUND => SM27_PROJECT_NOT_FOUND,
                SM28_PROJECT_NOT_ACTIVE => SM28_PROJECT_NOT_ACTIVE,
                SM29_USER_NOT_FOUND => SM29_USER_NOT_FOUND,
                SM30_READING_LOCKED => SM30_READING_LOCKED,
                SM31_READING_LIST_EMPTY => SM31_READING_LIST_EMPTY,
                SM32_READING_UPDATE_EMPTY => SM32_READING_UPDATE_EMPTY,
                SM33_METER_READING_CREATE_SUCCESS => SM33_METER_READING_CREATE_SUCCESS,
                SM34_READING_EXISTS_IN_PERIOD => SM34_READING_EXISTS_IN_PERIOD,
                SM35_READING_VALUE_TOO_LOW => SM35_READING_VALUE_TOO_LOW,
                SM36_INVOICE_LIST_SUCCESS => SM36_INVOICE_LIST_SUCCESS,
                SM37_PAYMENT_LINK_SUCCESS => SM37_PAYMENT_LINK_SUCCESS,
                SM38_INVOICE_GENERATE_SUCCESS => SM38_INVOICE_GENERATE_SUCCESS,
                SM39_PAYMENT_LINK_FAILED => SM39_PAYMENT_LINK_FAILED,
                SM40_SYSTEM_ERROR => SM40_SYSTEM_ERROR,
                SM41_INVOICE_DETAIL_SUCCESS => SM41_INVOICE_DETAIL_SUCCESS,
                _ => code
            };
        }

        public static ApiResponse Success(string message = "") => new() { Succeeded = true, Message = message };

        public static ApiResponse SuccessWithCode(string systemMessageCode, string? objectName = null, int? count = null)
        {
            string message = GetMessageFromCode(systemMessageCode);

            if (systemMessageCode == SM04_CREATE_SUCCESS && !string.IsNullOrEmpty(objectName))
            {
                message = message.Replace("{objectName}", objectName);
            }

            if (systemMessageCode == SM05_DELETION_SUCCESS && !string.IsNullOrEmpty(objectName))
            {
                message = message.Replace("{objectName}", objectName);
            }

            if (systemMessageCode == SM33_METER_READING_CREATE_SUCCESS && count.HasValue)
            {
                message = message.Replace("{count}", count.Value.ToString());
            }

            return new() { Succeeded = true, Message = message };
        }

        public static ApiResponse Fail(string code, string? fieldName = null)
        {
            string message = GetMessageFromCode(code);

            if (code == SM16_DUPLICATE_CODE && !string.IsNullOrEmpty(fieldName))
            {
                string formattedFieldName = char.ToUpper(fieldName[0]) + fieldName.Substring(1);

                message = message.Replace("{fieldName}", formattedFieldName);
            }

            return new() { Succeeded = false, Message = message };
        }
    }

    public sealed record ApiResponse<T>(T? Data) : ApiResponse
    {
        public static ApiResponse<T> Success(T data, string message = "") =>
            new(data) { Succeeded = true, Message = message };


        public static ApiResponse<T> SuccessWithCode(T data, string systemMessageCode, string? objectName = null)
        {
            string message = ApiResponse.GetMessageFromCode(systemMessageCode);

            if (systemMessageCode == SM04_CREATE_SUCCESS && !string.IsNullOrEmpty(objectName))
            {
                message = message.Replace("{objectName}", objectName);
            }

            return new(data) { Succeeded = true, Message = message };
        }

        public static new ApiResponse<T> Fail(string code, string? fieldName = null)
        {
            string message = GetMessageFromCode(code);


            // cấu hình đặc biệt cho SM16 với placeholder
            if (code == SM16_DUPLICATE_CODE && !string.IsNullOrEmpty(fieldName))
            {
                string formattedFieldName = char.ToUpper(fieldName[0]) + fieldName.Substring(1);

                message = message.Replace("{fieldName}", formattedFieldName);
            }


            return new((T?)default) { Succeeded = false, Message = message };
        }
    }
}