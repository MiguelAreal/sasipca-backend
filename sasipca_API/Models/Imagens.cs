using System;
using System.Collections.Generic;

namespace sasipca_API.Models;

public partial class Imagens
{
    public int IdImagem { get; set; }

    /// <summary>
    /// Link da imagem, guardado na conta de armazenamento Azure.
    /// </summary>
    public string Url { get; set; } = null!;

    public virtual ICollection<Produto> IdProduto { get; set; } = new List<Produto>();

    public virtual ICollection<Servico> IdServico { get; set; } = new List<Servico>();
}
