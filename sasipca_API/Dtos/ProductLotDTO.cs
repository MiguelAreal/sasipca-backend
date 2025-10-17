namespace sasipca_API.Dtos
{
    /// <summary>
    /// Utilizado para ir buscar detalhes de um lote de produto.
    /// </summary>
    public class ProductLotDTO
    {
        public int Id { get; set; }
        public string Lot { get; set; } = null!;
        public DateOnly ExpiryDate { get; set; }
        public int TotalQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableStock { get; set; }
    }
}