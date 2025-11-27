using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class DeliveryItem
{
    public int Id { get; set; }

    public int DeliveryId { get; set; }

    public int ProductGroupId { get; set; }

    public int Quantity { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Delivery Delivery { get; set; } = null!;

    public virtual ProductGroup ProductGroup { get; set; } = null!;
}
