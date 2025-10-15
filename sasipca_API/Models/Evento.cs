using System;
using System.Collections.Generic;

namespace sasipca_API.Models;

public partial class Evento
{
    public int IdEvento { get; set; }

    /// <summary>
    /// Nome/Título do Evento
    /// </summary>
    public string Nome { get; set; } = null!;

    /// <summary>
    /// Localização onde o evento decorre
    /// </summary>
    public string Morada { get; set; } = null!;

    /// <summary>
    /// Requisito de número mínimo de pessoas para o evento ocorrer
    /// </summary>
    public int? NumMinPessoas { get; set; }

    /// <summary>
    /// Descrição do evento
    /// </summary>
    public string? Descricao { get; set; }

    /// <summary>
    /// Data/Hora de início do evento.
    /// </summary>
    public DateTime DataIni { get; set; }

    /// <summary>
    /// ID da pessoa que criou o evento
    /// </summary>
    public int? IdEstado { get; set; }

    /// <summary>
    /// ID do estado do momento do evento.
    /// </summary>
    public int IdCriador { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;

    public virtual Pessoa IdCriadorNavigation { get; set; } = null!;

    public virtual EstadoEvento? IdEstadoNavigation { get; set; }

    public virtual ICollection<InscricaoEvento> InscricaoEvento { get; set; } = new List<InscricaoEvento>();

    public virtual ICollection<ItemNecessarioEvento> ItemNecessarioEvento { get; set; } = new List<ItemNecessarioEvento>();
}
