using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Proposal
{
    public string ProposalId { get; set; } = null!;

    public string ResidentId { get; set; } = null!;

    public string? OperationStaffId { get; set; }

    public string Content { get; set; } = null!;

    public string? Reply { get; set; }

    public string Status { get; set; } = null!;
    public string Tilte { get; set; } 
    public string Type { get; set; } 
    
    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? OperationStaff { get; set; }

    public virtual User Resident { get; set; } = null!;
}
