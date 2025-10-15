using System;
using System.Collections.Generic;

namespace sasipca_API.Models;

public partial class Servico
{
    public int IdServico { get; set; }

    /// <summary>
    /// Nome/Título do Serviço
    /// </summary>
    public string Nome { get; set; } = null!;

    /// <summary>
    /// Descrição do Serviço
    /// </summary>
    public string? Descricao { get; set; }

    /// <summary>
    /// Data/Hora para o Início do Serviço
    /// </summary>
    public DateTime DataIni { get; set; }

    /// <summary>
    /// Data/Hora de fim do Serviço (opcional)
    /// </summary>
    public DateTime? DataFim { get; set; } = null;

    /// <summary>
    /// Preço do Serviço (Por Hora/Total).
    /// </summary>
    public decimal Preco { get; set; }

    /// <summary>
    /// ID da pessoa que criou o serviço.
    /// </summary>
    public int IdCriador { get; set; }

    /// <summary>
    /// ID da pessoa que executou o serviço. Nulo porque quando o registo é criado ainda não existe propostas.
    /// </summary>
    public int? IdExecutor { get; set; }

    /// <summary>
    /// ID da avaliação dada a este serviço.
    /// </summary>
    public int? IdAvaliacao { get; set; }

    /// <summary>
    /// ID do estado no qual o serviço se encontra.
    /// </summary>
    public int? IdEstado { get; set; }

    /// <summary>
    /// ID do tipo da modalidade de pagamento deste serviço (Hora/Totall)
    /// </summary>
    public int IdModalidadepreco { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;

    public virtual Avaliacao? IdAvaliacaoNavigation { get; set; }

    public virtual Pessoa IdCriadorNavigation { get; set; } = null!;

    public virtual EstadoServico? IdEstadoNavigation { get; set; }

    public virtual Pessoa? IdExecutorNavigation { get; set; }

    public virtual ModalidadePreco IdModalidadeprecoNavigation { get; set; } = null!;

    public virtual ICollection<PropostaServico> PropostaServico { get; set; } = new List<PropostaServico>();

    public virtual ICollection<Imagens> IdImagem { get; set; } = new List<Imagens>();
}
