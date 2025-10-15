using System;
using System.Collections.Generic;

namespace sasipca_API.Models;

public partial class CategoriasProdutos
{
    public int IdCategoria { get; set; }

    /// <summary>
    /// Nome da categoria.
    /// </summary>
    public string? Nome { get; set; }

    public virtual ICollection<Produto> Produto { get; set; } = new List<Produto>();
}
