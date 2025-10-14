using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class PermissionGroup
{
    public string PermissionGroupId { get; set; } = null!;

    public string? Name { get; set; }

    public string? Permissions { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
