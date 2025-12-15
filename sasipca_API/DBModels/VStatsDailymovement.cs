using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class VStatsDailymovement
{
    public DateTime MovementDate { get; set; }

    public string Barcode { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public int? CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public string MovementType { get; set; } = null!;

    public int MovementTypeId { get; set; }

    public decimal? TotalQuantity { get; set; }
}
