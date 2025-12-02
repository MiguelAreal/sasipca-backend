using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class HangfireDistributedLock
{
    public string Resource { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
