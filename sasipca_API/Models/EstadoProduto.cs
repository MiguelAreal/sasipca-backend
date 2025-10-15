using System;
using System.Collections.Generic;

namespace sasipca_API.Models;

public partial class EstadoProduto
{
    public int IdEstado { get; set; }

    /// <summary>
    /// Tipo de estado de um produto.
    /// </summary>
    public string TipoEstado { get; set; } = null!;

    public virtual ICollection<Produto> Produto { get; set; } = new List<Produto>();
}
