namespace sasipca_API.Dtos
{

    /// <summary>
    /// Classe usada para retornar as notificações
    /// </summary>
    public class NotificationGetDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public bool IsRead { get; set; }
    }
}
