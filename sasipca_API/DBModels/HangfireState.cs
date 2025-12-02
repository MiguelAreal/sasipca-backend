using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class HangfireState
{
    public int Id { get; set; }

    public int JobId { get; set; }

    public string Name { get; set; } = null!;

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Data { get; set; }

    public virtual HangfireJob Job { get; set; } = null!;
}
