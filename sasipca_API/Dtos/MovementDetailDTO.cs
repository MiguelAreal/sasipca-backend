namespace sasipca_API.Dtos
{
    /// <summary>
    /// Estrutura de resposta para os detalhes completos de um Movimento.
    /// </summary>
    public class MovementDetailDTO
    {
        // Cabeçalho
        public int MovementId { get; set; }
        public DateTime MovementDate { get; set; }
        public string MovementType { get; set; } = null!;
        public string? MovementNote { get; set; }

        // Utilizador
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;

        // Entrega (Saída)
        public int? DeliveryId { get; set; }
        public DateTime? DeliveryScheduledDate { get; set; }
        public int? BeneficiaryId { get; set; }
        public string? BeneficiaryName { get; set; }

        // Itens
        public List<MovementItemDTO> Items { get; set; } = new List<MovementItemDTO>();
    }

}
