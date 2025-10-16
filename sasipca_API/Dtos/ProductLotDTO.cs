namespace sasipca_API.Dtos
{
    /// <summary>
    /// Utilizado para ir buscar detalhes de um lote de produto.
    /// </summary>
    public class ProductLotDTO
    {
        public int Id { get; set; }
        public string Lot { get; set; } = null!;
        public int Quantity { get; set; }
        public DateOnly ExpiryDate { get; set; }
    }
}