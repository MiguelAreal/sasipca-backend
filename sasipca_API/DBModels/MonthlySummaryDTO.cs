namespace sasipca_API.DBModels
{
    public class MonthlySummaryDTO
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public int PendingDeliveries { get; set; } // Entregas Pendentes
        public int RealizedDeliveries { get; set; } // Entregas Realizadas
        public int DonationsReceived { get; set; } // Doações Feitas (Entradas)
    }
}
