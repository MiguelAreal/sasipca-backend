using System;
using System.Collections.Generic;

namespace sasipca_API.Models;

public partial class EstadoEvento
{
    public int IdEstado { get; set; }

    /// <summary>
    /// Tipo de estado de um evento.
    /// </summary>
    public string TipoEstado { get; set; } = null!;

    public virtual ICollection<Evento> Evento { get; set; } = new List<Evento>();
}
