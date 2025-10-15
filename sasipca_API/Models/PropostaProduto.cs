using System;
using System.Collections.Generic;

namespace sasipca_API.Models;

public partial class PropostaProduto
{
    public int IdPropostaProduto { get; set; }

    /// <summary>
    /// Valor dado pelo comprador.
    /// </summary>
    public decimal Valor { get; set; }

    /// <summary>
    /// ID da pessoa a fazer a proposta.
    /// </summary>
    public int IdComprador { get; set; }

    /// <summary>
    /// ID do produto a ter proposta
    /// </summary>
    public int IdProduto { get; set; }

    /// <summary>
    /// ID do estado atual da proposta.
    /// </summary>
    public int IdEstado { get; set; }

    public virtual Pessoa IdCompradorNavigation { get; set; } = null!;

    public virtual EstadoProposta IdEstadoNavigation { get; set; } = null!;

    public virtual Produto IdProdutoNavigation { get; set; } = null!;
}
