using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class DeliveryStatus
{
    public int Id { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
}
