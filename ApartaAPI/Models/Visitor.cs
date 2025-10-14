using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Visitor
{
    public string VisitorId { get; set; } = null!;

    public string? FullName { get; set; }

    public string? Phone { get; set; }

    public string? IdNumber { get; set; }

    public virtual ICollection<VisitLog> VisitLogs { get; set; } = new List<VisitLog>();
}
