using System;
using System.Collections.Generic;

namespace sasipca_API.Models;

public partial class Produto
{
    public int IdProduto { get; set; }

    /// <summary>
    /// Nome de Produto.
    /// </summary>
    public string Nome { get; set; } = null!;

    /// <summary>
    /// Preço do produto.
    /// </summary>
    public decimal Preco { get; set; }

    /// <summary>
    /// ID da pessoa que fez o anúncio / está a vender
    /// </summary>
    public int IdVendedor { get; set; }

    /// <summary>
    /// ID da categoria ao qual este produto pertence.
    /// </summary>
    public int IdCategoria { get; set; }

    /// <summary>
    /// ID Referente ao estado atual do produto
    /// </summary>
    public int? IdEstado { get; set; }

    public string? Descricao { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;

    public virtual CategoriasProdutos IdCategoriaNavigation { get; set; } = null!;

    public virtual EstadoProduto? IdEstadoNavigation { get; set; }

    public virtual Pessoa IdVendedorNavigation { get; set; } = null!;

    public virtual ICollection<PropostaProduto> PropostaProduto { get; set; } = new List<PropostaProduto>();

    public virtual ICollection<Imagens> IdImagem { get; set; } = new List<Imagens>();
}
