namespace sasipca_API.Dtos
{

    /// <summary>
    /// Classe usada para retornar as notificações
    /// </summary>
    public class NotificationGetDTO
    {
        public int Id { get; set; }
        public string Message { get; set; }

        public string Status { get; set; }
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

    }
}
