using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Message
{
    public string MessageId { get; set; } = null!;

    public string InteractionId { get; set; } = null!;

    public string SenderId { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime SentAt { get; set; }

    public bool IsRead { get; set; }

    public virtual Interaction Interaction { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}
