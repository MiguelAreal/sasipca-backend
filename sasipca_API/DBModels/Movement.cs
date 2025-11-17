using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class Movement
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int MovementTypeId { get; set; }

    public int? DeliveryId { get; set; }

    /// <summary>
    /// Apenas se aplica quando movement_type é &apos;Receção&apos;
    /// </summary>
    public int? CampaignId { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Note { get; set; }

    public virtual Campaign? Campaign { get; set; }

    public virtual Delivery? Delivery { get; set; }

    public virtual ICollection<MovementItem> MovementItems { get; set; } = new List<MovementItem>();

    public virtual MovementType MovementType { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
