using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class Delivery
{
    public int Id { get; set; }

    public int BeneficiaryId { get; set; }

    public int UserId { get; set; }

    public int StatusId { get; set; }

    public string? Note { get; set; }

    public DateTime ScheduledDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Beneficiary Beneficiary { get; set; } = null!;

    public virtual ICollection<DeliveryItem> DeliveryItems { get; set; } = new List<DeliveryItem>();

    public virtual ICollection<Movement> Movements { get; set; } = new List<Movement>();

    public virtual DeliveryStatus Status { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
