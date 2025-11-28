using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class VDeliveriesDetail
{
    public int DeliveryId { get; set; }

    public DateOnly ScheduledDate { get; set; }

    public int StatusId { get; set; }

    public string? Note { get; set; }

    public int UserId { get; set; }

    public string? UserName { get; set; }

    public int BeneficiaryId { get; set; }

    public string BeneficiaryName { get; set; } = null!;

    public int DeliveryItemId { get; set; }

    public int ItemQuantity { get; set; }

    public DateTime? ItemCreatedAt { get; set; }

    public int ProductGroupId { get; set; }

    public DateOnly GroupExpiryDate { get; set; }

    public string ProductBarcode { get; set; } = null!;

    public string ProductName { get; set; } = null!;
}
