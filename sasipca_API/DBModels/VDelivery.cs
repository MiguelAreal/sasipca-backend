using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class VDelivery
{
    public int DeliveryId { get; set; }

    public DateOnly ScheduledDate { get; set; }

    public int StatusId { get; set; }

    public string? Note { get; set; }

    public int UserId { get; set; }

    public string? UserName { get; set; }

    public int BeneficiaryId { get; set; }

    public string BeneficiaryName { get; set; } = null!;
}
