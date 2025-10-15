using System;
using System.Collections.Generic;

namespace sasipca_API.Models;

/// <summary>
/// Registos de inscrição de pessoas a eventos, especificando também o item que selecionou para levar.
/// </summary>
public partial class InscricaoEvento
{
    public int IdInscricao { get; set; }

    /// <summary>
    /// ID do evento ao qual a pessoa se inscreve.
    /// </summary>
    public int IdEvento { get; set; }

    /// <summary>
    /// ID da pessoa à qual esta inscrição se aplica
    /// </summary>
    public int IdPessoa { get; set; }

    /// <summary>
    /// ID do item selecionado para a inscrição.
    /// </summary>
    public int? IdItem { get; set; }

    /// <summary>
    /// Data/Hora da inscrição a este evento.
    /// </summary>
    public DateTime DataInscricao { get; set; } = DateTime.Now;

    public virtual Evento IdEventoNavigation { get; set; } = null!;

    public virtual ItemNecessarioEvento IdItemNavigation { get; set; } = null!;

    public virtual Pessoa IdPessoaNavigation { get; set; } = null!;
}
