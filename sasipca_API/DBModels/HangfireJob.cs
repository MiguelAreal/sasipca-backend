using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class HangfireJob
{
    public int Id { get; set; }

    public int? StateId { get; set; }

    public string? StateName { get; set; }

    public string InvocationData { get; set; } = null!;

    public string Arguments { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpireAt { get; set; }

    public virtual ICollection<HangfireJobParameter> HangfireJobParameters { get; set; } = new List<HangfireJobParameter>();

    public virtual ICollection<HangfireJobState> HangfireJobStates { get; set; } = new List<HangfireJobState>();

    public virtual ICollection<HangfireState> HangfireStates { get; set; } = new List<HangfireState>();
}
