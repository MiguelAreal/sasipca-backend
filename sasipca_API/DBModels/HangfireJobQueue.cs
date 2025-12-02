using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class HangfireJobQueue
{
    public int Id { get; set; }

    public int JobId { get; set; }

    public DateTime? FetchedAt { get; set; }

    public string Queue { get; set; } = null!;

    public string? FetchToken { get; set; }
}
