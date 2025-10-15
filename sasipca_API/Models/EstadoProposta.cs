using System;
using System.Collections.Generic;

namespace sasipca_API.Models;

public partial class EstadoProposta
{
    public int IdEstado { get; set; }

    /// <summary>
    /// Descrição do estado da proposta.
    /// </summary>
    public string? TipoEstado { get; set; }

    public virtual ICollection<PropostaProduto> PropostaProduto { get; set; } = new List<PropostaProduto>();

    public virtual ICollection<PropostaServico> PropostaServico { get; set; } = new List<PropostaServico>();
}
