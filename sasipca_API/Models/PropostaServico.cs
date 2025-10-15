using System;
using System.Collections.Generic;

namespace sasipca_API.Models;

public partial class PropostaServico
{
    public int IdPropostaServico { get; set; }

    /// <summary>
    /// ID da pessoa a fazer a proposta.
    /// </summary>
    public int IdExecutor { get; set; }

    /// <summary>
    /// ID do serviço a ter proposta
    /// </summary>
    public int IdServico { get; set; }

    /// <summary>
    /// ID do estado atual da proposta.
    /// </summary>
    public int IdEstado { get; set; }

    public virtual EstadoProposta IdEstadoNavigation { get; set; } = null!;

    public virtual Pessoa IdExecutorNavigation { get; set; } = null!;

    public virtual Servico IdServicoNavigation { get; set; } = null!;
}
