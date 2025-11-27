namespace sasipca_API.Dtos
{
    /// <summary>
    /// Utilizado para ir buscar detalhes de um grupo de produto.
    /// </summary>
    public class ProductGroupDTO
    {
        public int Id { get; set; }
        public DateOnly ExpiryDate { get; set; }
        public int TotalQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableStock { get; set; }
    }
}