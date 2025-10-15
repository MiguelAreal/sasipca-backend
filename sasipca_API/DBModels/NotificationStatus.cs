using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class NotificationStatus
{
    public int Id { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
