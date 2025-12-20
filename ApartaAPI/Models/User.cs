using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class User
{
    public string UserId { get; set; } = null!;

    public string RoleId { get; set; } = null!;

    public string? ApartmentId { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string PasswordHash { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public string? StaffCode { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? LastLoginAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public string? PasswordResetToken { get; set; }

    public DateTime? ResetTokenExpires { get; set; }

    public bool IsFirstLogin { get; set; }

    public virtual Apartment? Apartment { get; set; }

    public virtual ICollection<ApartmentMember> ApartmentMembers { get; set; } = new List<ApartmentMember>();

    public virtual ICollection<Interaction> InteractionResidents { get; set; } = new List<Interaction>();

    public virtual ICollection<Interaction> InteractionStaffs { get; set; } = new List<Interaction>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();

    public virtual ICollection<News> News { get; set; } = new List<News>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<Proposal> ProposalOperationStaffs { get; set; } = new List<Proposal>();

    public virtual ICollection<Proposal> ProposalResidents { get; set; } = new List<Proposal>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<ServiceBooking> ServiceBookings { get; set; } = new List<ServiceBooking>();

    public virtual ICollection<StaffBuildingAssignment> StaffBuildingAssignmentAssignedByNavigations { get; set; } = new List<StaffBuildingAssignment>();

    public virtual ICollection<StaffBuildingAssignment> StaffBuildingAssignmentUsers { get; set; } = new List<StaffBuildingAssignment>();

    public virtual ICollection<TaskAssignment> TaskAssignmentAssigneeUsers { get; set; } = new List<TaskAssignment>();

    public virtual ICollection<TaskAssignment> TaskAssignmentAssignerUsers { get; set; } = new List<TaskAssignment>();

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    public virtual ICollection<UtilityBooking> UtilityBookings { get; set; } = new List<UtilityBooking>();
}
