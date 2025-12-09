using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class VStockPerGroup
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

    public int ProductGroupId { get; set; }

    public DateOnly ExpiryDate { get; set; }

    public int TotalQuantity { get; set; }

    public decimal ReservedQuantity { get; set; }

    public decimal AvailableStock { get; set; }
}
