using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class Report
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int CreatorId { get; set; }

    public DateTime CreatedAt { get; set; }

    public int ReportType { get; set; }

    public virtual User Creator { get; set; } = null!;

    public virtual ReportType ReportTypeNavigation { get; set; } = null!;
}
