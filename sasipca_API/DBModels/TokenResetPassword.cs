using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class TokenResetPassword
{
    public int UserId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime ExpDate { get; set; }

    public virtual User User { get; set; } = null!;
}
