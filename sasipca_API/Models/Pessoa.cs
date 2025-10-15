using System;
using System.Collections.Generic;

namespace sasipca_API.Models;

/// <summary>
/// Tabela para utilizadores.
/// </summary>
public partial class Pessoa
{
    public int IdPessoa { get; set; }

    /// <summary>
    /// Nome da pessoa.
    /// </summary>
    public string Nome { get; set; } = null!;

    /// <summary>
    /// Morada da pessoa.
    /// </summary>
    public string Morada { get; set; } = null!;

    /// <summary>
    /// Endereço de E-Mail da pessoa.
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// Palavra-Passe encriptada por SHA-256 da pessoa.
    /// </summary>
    public string Password { get; set; } = null!;

    /// <summary>
    /// Contacto Telefónico da pessoa.
    /// </summary>
    public string Contacto { get; set; } = null!;

    /// <summary>
    /// Código Postal da pessoa.
    /// </summary>
    public string? IdCodPostal { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiryTime { get; set; }

    public DateTime? DataCriacao { get; set; } = DateTime.Now;

    public virtual ICollection<Avaliacao> Avaliacao { get; set; } = new List<Avaliacao>();

    public virtual ICollection<Evento> Evento { get; set; } = new List<Evento>();

    public virtual CodigoPostal? IdCodPostalNavigation { get; set; }

    public virtual ICollection<InscricaoEvento> InscricaoEvento { get; set; } = new List<InscricaoEvento>();

    public virtual ICollection<Notificacao> Notificacao { get; set; } = new List<Notificacao>();

    public virtual ICollection<Produto> Produto { get; set; } = new List<Produto>();

    public virtual ICollection<PropostaProduto> PropostaProduto { get; set; } = new List<PropostaProduto>();

    public virtual ICollection<PropostaServico> PropostaServico { get; set; } = new List<PropostaServico>();

    public virtual ICollection<Servico> ServicoIdCriadorNavigation { get; set; } = new List<Servico>();

    public virtual ICollection<Servico> ServicoIdExecutorNavigation { get; set; } = new List<Servico>();

    //public virtual ICollection<TokenResetPassword> TokenResetPassword { get; set; } = new List<TokenResetPassword>();
}
