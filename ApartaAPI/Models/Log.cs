using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Log
{
    public string LogId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string Action { get; set; } = null!;

    public DateTime Timestamp { get; set; }

    public string? Details { get; set; }

    public virtual User User { get; set; } = null!;
}
