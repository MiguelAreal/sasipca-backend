namespace sasipca_API.Dtos
{
    public class DashboardSummaryDTO
    {
        public int TotalProductsInStock { get; set; }
        public int LowStockCount { get; set; } // Produtos abaixo do nível de aviso
        public int PendingDeliveriesCount { get; set; } // Agendadas
        public int ActiveBeneficiariesCount { get; set; } // Últimos 30 dias
    }
}
