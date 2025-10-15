using System.ComponentModel.DataAnnotations;

namespace sasipca_API.Dtos
{


    /// <summary>
    /// DTO base para os detalhes de um Serviço.
    /// </summary>
    public class ServicoDTO
    {
        /// <summary>
        /// Nome do Serviço.
        /// </summary>
        [Required]
        public string Nome { get; set; } = null!;

        /// <summary>
        /// Preço do Serviço.
        /// </summary>
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor do preço deve ser maior que zero.")]
        public decimal Preco { get; set; }

        /// <summary>
        /// Descrição do Serviço (opcional).
        /// </summary>
        public string? Descricao { get; set; }

        /// <summary>
        /// Data/Hora do Início do Serviço.
        /// </summary>
        public DateTime DataIni { get; set; }

        /// <summary>
        /// Data/Hora de Término do Serviço (opcional).
        /// </summary>
        public DateTime? DataFim { get; set; } = null;



    }

}
