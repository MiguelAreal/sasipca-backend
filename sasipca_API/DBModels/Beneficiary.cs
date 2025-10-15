using System;
using System.Collections.Generic;

namespace sasipca_API.DBModels;

public partial class Beneficiary
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Contact { get; set; } = null!;

    public string Course { get; set; } = null!;

    public int CurricularYear { get; set; }

    public int? AddressId { get; set; }

    public string? GlobalObs { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int CreatedBy { get; set; }

    public virtual BeneficiaryAddress? Address { get; set; }

    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();

    public virtual ICollection<ParticularOb> ParticularObs { get; set; } = new List<ParticularOb>();
}
