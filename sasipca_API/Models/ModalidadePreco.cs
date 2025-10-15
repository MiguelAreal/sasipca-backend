using System;
using System.Collections.Generic;

namespace sasipca_API.Models;

public partial class ModalidadePreco
{
    public int IdModalidade { get; set; }

    /// <summary>
    /// Se o pagamento de um serviço é Total/Hora/Outros.
    /// </summary>
    public string Tipo { get; set; } = null!;

    public virtual ICollection<Servico> Servico { get; set; } = new List<Servico>();
}
