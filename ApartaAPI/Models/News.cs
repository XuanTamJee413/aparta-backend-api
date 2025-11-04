using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class News
{
    public string NewsId { get; set; } = null!;

    public string AuthorUserId { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime? PublishedDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Status { get; set; }

    public virtual User AuthorUser { get; set; } = null!;
}
