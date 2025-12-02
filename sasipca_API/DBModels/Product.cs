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

    /// <summary>
    /// Com quantos dias de antecência deve avisar que um grupo do produto vai expirar
    /// </summary>
    public int? ExpNotif { get; set; }

    public virtual CategoryType Category { get; set; } = null!;

    public virtual ICollection<ProductGroup> ProductGroups { get; set; } = new List<ProductGroup>();

    public virtual UnitType Unit { get; set; } = null!;
}
