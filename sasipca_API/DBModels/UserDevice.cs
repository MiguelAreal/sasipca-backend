using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class UserDevice
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string FcmToken { get; set; } = null!;

    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
