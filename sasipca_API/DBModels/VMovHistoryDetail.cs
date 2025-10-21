using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class VMovHistoryDetail
{
    public int MovementId { get; set; }

    public DateTime MovementDate { get; set; }

    public string MovementType { get; set; } = null!;

    public string? MovementNote { get; set; }

    public int ItemQuantityAffected { get; set; }

    public string ProductLotNumber { get; set; } = null!;

    public DateOnly LotExpiryDate { get; set; }

    public string ProductBarcode { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public int UserId { get; set; }

    public string UserName { get; set; } = null!;

    public int? DeliveryId { get; set; }

    public DateOnly? DeliveryScheduledDate { get; set; }

    public int? BeneficiaryId { get; set; }

    public string? BeneficiaryName { get; set; }
}
