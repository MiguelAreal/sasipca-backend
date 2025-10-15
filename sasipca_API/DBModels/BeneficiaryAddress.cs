using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class BeneficiaryAddress
{
    public int Id { get; set; }

    public string Street { get; set; } = null!;

    public int Number { get; set; }

    public string PostalCode { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Beneficiary> Beneficiaries { get; set; } = new List<Beneficiary>();
}
