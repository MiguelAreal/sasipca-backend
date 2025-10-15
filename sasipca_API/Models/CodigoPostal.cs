using System;
using System.Collections.Generic;

namespace sasipca_API.Models;

public partial class CodigoPostal
{
    /// <summary>
    /// O próprio código postal
    /// </summary>
    public string IdCodPostal { get; set; } = null!;

    /// <summary>
    /// Localidade ao qual o código-postal corresponde.
    /// </summary>
    public string Localidade { get; set; } = null!;

    public virtual ICollection<Pessoa> Pessoa { get; set; } = new List<Pessoa>();
}
