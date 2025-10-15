using System;
using System.Collections.Generic;

namespace sasipca_API.Models;

public partial class ItemNecessarioEvento
{
    public int IdItem { get; set; }

    /// <summary>
    /// Evento ao qual este item pertence.
    /// </summary>
    public int IdEvento { get; set; }

    /// <summary>
    /// Nome do item.
    /// </summary>
    public string Nome { get; set; } = null!;

    /// <summary>
    /// Quantidade necessária deste item para este evento.
    /// </summary>
    public int? Quantidade { get; set; }

    public virtual Evento IdEventoNavigation { get; set; } = null!;

    public virtual ICollection<InscricaoEvento> InscricaoEvento { get; set; } = new List<InscricaoEvento>();
}
