using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class VStockPerProduct
{
    public string Barcode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string CategoryType { get; set; } = null!;

    public string UnitType { get; set; } = null!;

    public int? UnitSize { get; set; }

    public decimal TotalQuantity { get; set; }

    public decimal ReservedQuantity { get; set; }

    public decimal AvailableStock { get; set; }
}
