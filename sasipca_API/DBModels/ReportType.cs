using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class ReportType
{
    public int Id { get; set; }

    public string Type { get; set; } = null!;

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
}
