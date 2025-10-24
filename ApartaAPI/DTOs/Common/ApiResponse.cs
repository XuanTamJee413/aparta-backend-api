namespace ApartaAPI.DTOs.Common
{
    public record ApiResponse
    {
        public bool Succeeded { get; init; }
        public string Message { get; init; } = string.Empty;


        public const string SM01_NO_RESULTS = "No matching results found.";
        public const string SM02_REQUIRED = "This field is required.";
        public const string SM03_UPDATE_SUCCESS = "Information updated successfully.";
        public const string SM04_CREATE_SUCCESS = "New {objectName} added successfully.";
        public const string SM05_DELETION_SUCCESS = "{objectName} deleted successfully.";
        public const string SM06_TASK_ASSIGN_SUCCESS = "Task assigned successfully.";
        public const string SM07_LOGIN_FAIL = "Incorrect username or password. Please check again.";
        public const string SM08_EXCEEDED_LENGTH = "Exceeded maximum length of {max_length} characters.";
        public const string SM09_INVALID_BOOKING = "Another resident has already booked this time slot. Please choose another time.";
        public const string SM10_PAYMENT_SUCCESS = "Invoice payment successful.";
        public const string SM11_INDEX_RECORD_SUCCESS = "Utility index recorded successfully.";
        public const string SM12_DEBT_WARNING = "This apartment has overdue debt. Please make a payment to avoid service restrictions.";
        public const string SM13_ACCOUNT_NOT_FOUND = "Account does not exist. Please check the information.";
        public const string SM14_DECLARATION_SUCCESS = "Visitor/Temporary stay declaration successful.";
        public const string SM15_PAYMENT_FAILED = "Payment failed. Please try again or contact support.";
        public const string SM16_DUPLICATE_CODE = "{fieldName} already exists. Please use a different value."; // Updated for dynamic messages
        public const string SM17_PERMISSION_DENIED = "You don’t have permission to perform this action.";
        public const string SM18_IMPORT_SUCCESS = "Data imported successfully.";
        public const string SM19_EXPORT_SUCCESS = "Data exported successfully.";
        public const string SM20_NO_CHANGES = "No changes detected.";
        public const string SM21_DELETION_FAILED = "Cannot delete. This item is currently in use or has dependencies.";
        public const string SM22_INVALID_COUPON = "Coupon not found or invalid.";
        public const string SM23_IMPORT_FAILED = "Data import failed. Please check the file format and data.";

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
                _ => code 
            };
        }

        public static ApiResponse Success(string message = "") => new() { Succeeded = true, Message = message };

        public static ApiResponse SuccessWithCode(string systemMessageCode, string? objectName = null)
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
        public static new ApiResponse<T> Success(T data, string message = "") =>
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