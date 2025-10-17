using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class Product
{
    public string Barcode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int UnitId { get; set; }

    public int CategoryId { get; set; }

    public int? UnitSize { get; set; }

    public virtual CategoryType Category { get; set; } = null!;

    public virtual ICollection<ProductLot> ProductLots { get; set; } = new List<ProductLot>();

    public virtual UnitType Unit { get; set; } = null!;
}
