using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class VAvailableStockPerProduct
{
    public string Barcode { get; set; } = null!;

    public decimal TotalQuantity { get; set; }

    public decimal ReservedQuantity { get; set; }

    public decimal AvailableStock { get; set; }
}
