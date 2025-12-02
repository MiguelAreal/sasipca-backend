using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class HangfireServer
{
    public string Id { get; set; } = null!;

    public string Data { get; set; } = null!;

    public DateTime? LastHeartbeat { get; set; }
}
