using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class News
{
    public string NewsId { get; set; } = null!;

    public string? Title { get; set; }

    public string? Content { get; set; }

    public string? ManagementStaffId { get; set; }

    public DateTime? PublishedDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? ManagementStaff { get; set; }
}
