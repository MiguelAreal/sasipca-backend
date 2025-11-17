using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class Campaign
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? ImageUrl { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Location { get; set; }

    public DateOnly EndDate { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateOnly StartDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Movement> Movements { get; set; } = new List<Movement>();

    public virtual User? User { get; set; }
}
