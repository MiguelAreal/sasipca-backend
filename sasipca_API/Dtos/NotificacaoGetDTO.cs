namespace sasipca_API.Dtos
{

    /// <summary>
    /// Classe usada para retornar as notificações
    /// </summary>
    public class NotificacaoGetDTO
    {
        public int IdNotificacao { get; set; }
        public string Mensagem { get; set; }
        public DateTime? DataCriacao { get; set; } = DateTime.Now;

    }
}
