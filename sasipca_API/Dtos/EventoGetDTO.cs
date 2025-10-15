using sasipca_API.Models;

namespace sasipca_API.Dtos
{

    /// <summary>
    /// Classe DTO para ir buscar eventos.
    /// </summary>
    public class EventoGetDTO : EventoDTO
    {
        public int IdEvento { get; set; }

        /// <summary>
        /// Identifica se o utilizador que fez o pedido é ou não o criador do evento.
        /// </summary>
        public bool IsCriador { get; set; }

        /// <summary>
        /// Identificador do estado em que o evento se encontra.
        /// </summary>
        public int? IdEstado { get; set; }

        /// <summary>
        /// Objeto de Criador (Pessoa) de um produto.
        /// </summary>
        public PessoaSimpleDTO Criador { get; set; } = null!;
        public List<ItemNecessarioGetDTO> ItensNecessarios { get; set; } = new();
        public DateTime DataCriacao { get; set; }
    }
}
