using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using sasipca_API.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace sasipca_API.Hubs
{
    [Authorize] // Adiciona autenticação ao hub
    public class NotificationHub : Hub
    {
        // Dicionário para mapear utilizadores às suas conexões
        private static readonly Dictionary<int, HashSet<string>> _userConnections = new();
        private static readonly object _lock = new();
        private readonly NotificacaoService _notifService;

        // Injeção de dependência via construtor
        public NotificationHub(NotificacaoService notifService)
        {
            _notifService = notifService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                Context.Abort(); // Encerra a conexão se não autenticado
                return;
            }

            lock (_lock)
            {
                if (!_userConnections.ContainsKey(userIdInt))
                {
                    _userConnections[userIdInt] = new HashSet<string>();
                }
                _userConnections[userIdInt].Add(Context.ConnectionId);
            }

            // Fetch and send notifications to the user upon connection
            var notifications = await _notifService.ObterNotificacoesUser(userIdInt);
            await Clients.Caller.SendAsync("LoadNotifications", notifications);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                lock (_lock)
                {
                    if (_userConnections.ContainsKey(userIdInt))
                    {
                        _userConnections[userIdInt].Remove(Context.ConnectionId);
                        if (_userConnections[userIdInt].Count == 0)
                        {
                            _userConnections.Remove(userIdInt);
                        }
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Método estático para enviar notificação para um utilizador específico
        public static HashSet<string> GetUserConnections(int userId)
        {
            lock (_lock)
            {
                return _userConnections.ContainsKey(userId)
                    ? new HashSet<string>(_userConnections[userId])
                    : new HashSet<string>();
            }
        }

        /// <summary>
        /// Método para apagar uma notificação específica.
        /// </summary>
        /// <param name="notificationId">ID da notificação a apagar.</param>
        /// <returns>Tarefa assíncrona</returns>
        public async Task DeleteNotification(int notificationId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return;
            }

            var success = await _notifService.DeleteNotificacao(notificationId, userIdInt);
            if (success)
            {
                await Clients.Caller.SendAsync("NotificationDeleted", notificationId);
            }
        }

        /// <summary>
        /// Método para apagar todas as notificações do utilizador conectado.
        /// </summary>
        /// <returns>Tarefa assíncrona</returns>
        public async Task DeleteAllNotifications()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return;
            }

            var success = await _notifService.DeleteAllNotificacoes(userIdInt);
            if (success)
            {
                await Clients.Caller.SendAsync("AllNotificationsDeleted");
            }
        }
    }
}