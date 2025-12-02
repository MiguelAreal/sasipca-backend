using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class HangfireAggregatedCounter
{
    public int Id { get; set; }

    public string Key { get; set; } = null!;

    public int Value { get; set; }

    public DateTime? ExpireAt { get; set; }
}
