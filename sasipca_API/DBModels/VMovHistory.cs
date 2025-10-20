using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class VMovHistory
{
    public int MovementId { get; set; }

    public DateTime MovementDate { get; set; }

    public string MovementType { get; set; } = null!;

    public string? MovementNote { get; set; }

    public int UserId { get; set; }

    public string UserName { get; set; } = null!;

    public int? DeliveryId { get; set; }

    public decimal? TotalQuantityAffected { get; set; }
}
