using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class ProductGroup
{
    public int Id { get; set; }

    public string Barcode { get; set; } = null!;

    public DateOnly ExpiryDate { get; set; }

    public int Quantity { get; set; }

    public virtual Product BarcodeNavigation { get; set; } = null!;

    public virtual ICollection<DeliveryItem> DeliveryItems { get; set; } = new List<DeliveryItem>();

    public virtual ICollection<MovementItem> MovementItems { get; set; } = new List<MovementItem>();
}
