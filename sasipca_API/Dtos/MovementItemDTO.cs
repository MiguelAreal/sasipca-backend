namespace sasipca_API.Dtos
{
    /// <summary>
    /// Estrutura de resposta para cada item de lote afetado num Movimento.
    /// </summary>
    public class MovementItemDTO
    {
        public int ItemQuantityAffected { get; set; }
        public string ProductBarcode { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public int ProductGroupId { get; set; }
        public DateOnly GroupExpiryDate { get; set; }
    }
}
