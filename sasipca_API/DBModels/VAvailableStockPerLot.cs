using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class VAvailableStockPerLot
{
    public int ProductLotId { get; set; }

    public string Barcode { get; set; } = null!;

    public string Lot { get; set; } = null!;

    public DateOnly ExpiryDate { get; set; }

    public int TotalQuantity { get; set; }

    public decimal ReservedQuantity { get; set; }

    public decimal AvailableStock { get; set; }
}
