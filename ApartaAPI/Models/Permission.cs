using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Permission
{
    public string PermissionId { get; set; } = null!;

    public string PermissionGroupId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual PermissionGroup PermissionGroup { get; set; } = null!;

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
