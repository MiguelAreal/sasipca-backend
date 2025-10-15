using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class ParticularOb
{
    public int UserId { get; set; }

    public int BeneficiaryId { get; set; }

    public string? Obs { get; set; }

    public virtual Beneficiary Beneficiary { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
