using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class VStockPerLot
{
    public string Barcode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int UnitId { get; set; }

    public int CategoryId { get; set; }

    public int? UnitSize { get; set; }

    public int ProductLotId { get; set; }

    public string Lot { get; set; } = null!;

    public DateOnly ExpiryDate { get; set; }

    public int TotalQuantity { get; set; }

    public decimal ReservedQuantity { get; set; }

    public decimal AvailableStock { get; set; }
}
