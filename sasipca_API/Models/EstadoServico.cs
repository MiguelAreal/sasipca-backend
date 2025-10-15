using System;
using System.Collections.Generic;

namespace sasipca_API.Models;

public partial class EstadoServico
{
    public int IdEstado { get; set; }

    /// <summary>
    /// Tipo de estado de um serviço.
    /// </summary>
    public string TipoEstado { get; set; } = null!;

    public virtual ICollection<Servico> Servico { get; set; } = new List<Servico>();
}
