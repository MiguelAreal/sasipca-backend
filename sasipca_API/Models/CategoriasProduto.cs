using System;
using System.Collections.Generic;

namespace sasipca_API.Models;

public partial class CategoriasProduto
{
    public int IdCategoria { get; set; }

    /// <summary>
    /// Nome da categoria.
    /// </summary>
    public string? Nome { get; set; }

    public virtual ICollection<Produto> Produtos { get; set; } = new List<Produto>();
}
