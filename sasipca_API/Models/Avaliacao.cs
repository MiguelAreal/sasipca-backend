using System;
using System.Collections.Generic;

namespace sasipca_API.Models;

public partial class Avaliacao
{
    public int IdAvaliacao { get; set; }

    /// <summary>
    /// Utilizador que fez a avaliação.
    /// </summary>
    public int IdUtilizador { get; set; }

    /// <summary>
    /// Nota dada a um serviço de 1 a 5.
    /// </summary>
    public int Nota { get; set; }

    /// <summary>
    /// Data/Hora que a avaliação foi criada.
    /// </summary>
    public DateTime DataAvaliacao { get; set; } = DateTime.Now;

    public virtual Pessoa IdUtilizadorNavigation { get; set; } = null!;

    public virtual ICollection<Servico> Servico { get; set; } = new List<Servico>();
}
