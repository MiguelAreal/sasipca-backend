using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class HangfireJobParameter
{
    public int Id { get; set; }

    public int JobId { get; set; }

    public string Name { get; set; } = null!;

    public string? Value { get; set; }

    public virtual HangfireJob Job { get; set; } = null!;
}
