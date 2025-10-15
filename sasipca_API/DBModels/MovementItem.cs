using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class MovementItem
{
    public int Id { get; set; }

    public int MovementId { get; set; }

    public int ProductLotId { get; set; }

    public int Quantity { get; set; }

    public virtual Movement Movement { get; set; } = null!;

    public virtual ProductLot ProductLot { get; set; } = null!;
}
