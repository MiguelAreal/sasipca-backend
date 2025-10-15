using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class MovementType
{
    public int Id { get; set; }

    public string Type { get; set; } = null!;

    public virtual ICollection<Movement> Movements { get; set; } = new List<Movement>();
}
