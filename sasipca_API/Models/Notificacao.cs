using System;
using System.Collections.Generic;

namespace sasipca_API.Models;

public partial class Notificacao
{
    public int IdNotificacao { get; set; }

    public int IdPessoa { get; set; }

    public string? Mensagem { get; set; }

    public DateTime? DataCriacao { get; set; } = DateTime.Now;

    public virtual Pessoa IdPessoaNavigation { get; set; } = null!;
}
