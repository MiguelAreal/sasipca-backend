namespace sasipca_API.Dtos
{
    /// <summary>
    /// Estrutura de resposta para os detalhes completos de uma entrega.
    /// </summary>
    public class DeliveryDetailDTO
    {
        // Cabeçalho
        public int DeliveryId { get; set; }
        public DateOnly ScheduledDate { get; set; }
        public int StatusId { get; set; }
        public string? Note { get; set; }

        // Utilizador
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;

        // Beneficiário
        public int? BeneficiaryId { get; set; }
        public string? BeneficiaryName { get; set; }

        // Itens
        public List<DeliveryItemGetDTO> Items { get; set; } = new List<DeliveryItemGetDTO>();
    }

}
