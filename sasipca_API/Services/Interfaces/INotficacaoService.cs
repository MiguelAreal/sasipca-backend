using sasipca_API.Dtos;

namespace sasipca_API.Services.Interfaces
{
    public interface INotificacaoService
    {
        Task<List<NotificacaoGetDTO>> ObterNotificacoesUser(int idUser);
        Task<bool> CriarNotificacao(int idUser, string mensagem);
        Task<bool> DeleteNotificacao(int idNotificacao, int idUser);
        Task<bool> DeleteAllNotificacoes(int idUser);
    }
}
