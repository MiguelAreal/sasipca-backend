using sasipca_API.Models;

namespace sasipca_API.Dtos
{

    /// <summary>
    /// Classe para criar um novo evento.
    /// </summary>
    public class EventoPostDTO : EventoDTO
    {
        public List<ItemNecessarioPostDTO> ItensNecessarios { get; set; } = new();
    }
}
