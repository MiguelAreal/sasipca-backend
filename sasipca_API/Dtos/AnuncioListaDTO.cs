using System.ComponentModel.DataAnnotations;

namespace sasipca_API.Dtos
{
    /// <summary>
    /// Classe base de Data Transfer Object para lista de anúncios (Produtos,eventos e serviços).
    /// </summary>
    public class AnuncioListaDTO
    {
        /// <summary>
        /// Identificador do Produto, Serviço ou Evento.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome do Produto, Serviço ou Evento.
        /// </summary>
        [Required]
        public string Nome { get; set; } = null!;

        /// <summary>
        /// Preço do Produto ou Serviço.
        /// Para Eventos: null.
        /// </summary>
        public decimal? Preco { get; set; }

        /// <summary>
        /// Modalidade de Preço do Produto ou Serviço.
        /// Para produtos: "Total".
        /// Para serviços: "À hora" / "Total".
        /// Para eventos: null.
        /// </summary>
        public string? TipoPreco { get; set; }

        /// <summary>
        /// Imagem de capa do Produto, Serviço ou Evento.
        /// Definido pela primeira imagem que o utilizador registou ao criar o anúncio.
        /// </summary>
        public string? ImagemUrl { get; set; }

        /// <summary>
        /// Categoria do anúncio. Identifica se este é um 'Produto', 'Serviço' ou 'Evento'.
        /// </summary>
        [Required]
        public string Categoria { get; set; } = null!;


        /// <summary>
        /// Data de criação do anúncio
        /// </summary>
        [Required]
        public DateTime DataCriacao { get; set; }
    }
}

// Classe para a resposta paginada
/*public class PaginatedResponse<T>
{
    public IEnumerable<T> Data { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}*/