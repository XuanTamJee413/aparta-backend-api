using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Propose
{
    public string ProposeId { get; set; } = null!;

    public string? ResidentId { get; set; }

    public string? Content { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? Resident { get; set; }
}
