namespace ApartaAPI.DTOs.Common
{
    public record ApiResponse
    {
        public bool Succeeded { get; init; }
        public string Message { get; init; } = string.Empty;

        public static ApiResponse Success(string message = "") => new() { Succeeded = true, Message = message };
        public static ApiResponse Fail(string message) => new() { Succeeded = false, Message = message };
    }

    /// <summary>
    /// Một response chuẩn của API, chứa data generic.
    /// </summary>
    public sealed record ApiResponse<T>(T? Data) : ApiResponse
    {
        public static ApiResponse<T> Success(T data, string message = "") => new(data) { Succeeded = true, Message = message };

        public static new ApiResponse<T> Fail(string message) => new((T?)default) { Succeeded = false, Message = message };
    }
}
