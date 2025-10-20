using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class Notification
{
    public int Id { get; set; }

    /// <summary>
    /// Create Time
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    public int StatusId { get; set; }

    /// <summary>
    /// User that the notification is for
    /// </summary>
    public int UserId { get; set; }

    public string? Message { get; set; }

    public virtual NotificationStatus Status { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
