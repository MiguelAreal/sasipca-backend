using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class UnitType
{
    public int Id { get; set; }

    public string Type { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
