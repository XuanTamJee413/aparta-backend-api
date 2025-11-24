using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.StaffAssignments;

namespace ApartaAPI.Services.Interfaces
{
    public interface IStaffAssignmentService
    {
        Task<ApiResponse<PaginatedResult<StaffAssignmentDto>>> GetAssignmentsAsync(StaffAssignmentQueryParameters query);
        Task<ApiResponse<StaffAssignmentDto>> AssignStaffAsync(StaffAssignmentCreateDto dto, string managerId);
        Task<ApiResponse<StaffAssignmentDto>> UpdateAssignmentAsync(string assignmentId, StaffAssignmentUpdateDto dto, string managerId);
        Task<ApiResponse> DeactivateAssignmentAsync(string assignmentId);
        Task<ApiResponse> DeletePermanentAsync(string assignmentId);
        Task<ApiResponse<IEnumerable<StaffUserDto>>> GetAvailableStaffsAsync(string? searchTerm);
        Task<ApiResponse<IEnumerable<StaffAssignmentBuildingDto>>> GetAvailableBuildingsAsync(string? searchTerm);
    }
}