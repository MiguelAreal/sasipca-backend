using Microsoft.EntityFrameworkCore;
using sasipca_API.Data;
using sasipca_API.Hubs;
using sasipca_API.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using sasipca_API.Dtos;
using sasipca_API.Services.Interfaces;

namespace sasipca_API.Services
{
    public class NotificacaoService : INotificacaoService
    {
        private readonly NLDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificacaoService(NLDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<List<NotificacaoGetDTO>> ObterNotificacoesUser(int idUser)
        {
            var notificacoes = await _context.Notificacao
                .Where(n => n.IdPessoa == idUser)
                .OrderByDescending(n => n.DataCriacao)
                .Select(n => new NotificacaoGetDTO
                {
                    IdNotificacao = n.IdNotificacao,
                    Mensagem = n.Mensagem,
                    DataCriacao = n.DataCriacao,
                })
                .ToListAsync();

            return notificacoes;
        }

        public async Task<bool> CriarNotificacao(int idUser, string mensagem)
        {
            try
            {
                var notificacao = new Notificacao
                {
                    IdPessoa = idUser,
                    Mensagem = mensagem,
                    DataCriacao = DateTime.Now
                };

                _context.Notificacao.Add(notificacao);
                await _context.SaveChangesAsync();

                var notifDTO = new NotificacaoGetDTO
                {
                    IdNotificacao = notificacao.IdNotificacao,
                    Mensagem = notificacao.Mensagem,
                    DataCriacao = notificacao.DataCriacao
                };

                var connections = NotificationHub.GetUserConnections(idUser);

                if (connections.Any())
                {
                    foreach (var connectionId in connections)
                    {
                        await _hubContext.Clients.Client(connectionId)
                            .SendAsync("ReceiveNotification", notifDTO);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao criar notificação: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteNotificacao(int idNotificacao, int idUser)
        {
            try
            {
                var notificacao = await _context.Notificacao
                    .FirstOrDefaultAsync(n => n.IdNotificacao == idNotificacao && n.IdPessoa == idUser);

                if (notificacao == null)
                    return false;

                _context.Notificacao.Remove(notificacao);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao apagar notificação: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteAllNotificacoes(int idUser)
        {
            try
            {
                var notificacoes = await _context.Notificacao
                    .Where(n => n.IdPessoa == idUser)
                    .ToListAsync();

                if (!notificacoes.Any())
                    return true;

                _context.Notificacao.RemoveRange(notificacoes);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao apagar todas as notificações: {ex.Message}");
                return false;
            }
        }
    }
}
