using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Interaction
{
    public string InteractionId { get; set; } = null!;

    public string StaffId { get; set; } = null!;

    public string ResidentId { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual User Resident { get; set; } = null!;

    public virtual User Staff { get; set; } = null!;
}
