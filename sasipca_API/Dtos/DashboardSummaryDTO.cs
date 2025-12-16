namespace sasipca_API.Dtos
{
    public class DashboardSummaryDTO
    {
        public int TotalProductsInStock { get; set; }
        public int PendingDeliveriesCount { get; set; } // Mantido a pedido
        public int ExpiredStockQuantity { get; set; }   // Stock Expirado
        public int NewBeneficiariesCount { get; set; }  // Novos registos no período
    }
}