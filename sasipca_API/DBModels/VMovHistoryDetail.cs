using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class VMovHistoryDetail
{
    public int MovementId { get; set; }

    public DateTime MovementDate { get; set; }

    public int MovementTypeId { get; set; }

    public string? MovementNote { get; set; }

    public int UserId { get; set; }

    public string? UserName { get; set; }

    public int? DeliveryId { get; set; }

    public int MovementItemId { get; set; }

    public int ItemQuantityAffected { get; set; }

    public int ProductLotId { get; set; }

    public string ProductLotNumber { get; set; } = null!;

    public DateOnly LotExpiryDate { get; set; }

    public string ProductBarcode { get; set; } = null!;

    public string ProductName { get; set; } = null!;
}
