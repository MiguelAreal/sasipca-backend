using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class HangfireHash
{
    public int Id { get; set; }

    public string Key { get; set; } = null!;

    public string Field { get; set; } = null!;

    public string? Value { get; set; }

    public DateTime? ExpireAt { get; set; }
}
