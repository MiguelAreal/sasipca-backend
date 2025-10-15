using Hangfire;
using Microsoft.EntityFrameworkCore;
using sasipca_API.Data;
using sasipca_API.Enumerators;
using sasipca_API.Models;
using sasipca_API.Services.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace sasipca_API.Services
{
    /// <summary>
    /// Serviço responsável por agendar a atualização do estado de eventos e serviços com base nas suas datas de início.
    /// </summary>
    public class JobSchedulerService : IJobSchedulerService
    {
        private readonly NLDbContext _dbcontext;
        private readonly INotificacaoService _notifService;
        private readonly string _logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "JobSchedulerService_logs.txt");

        /// <summary>
        /// Construtor que inicializa o serviço com o contexto da base de dados.
        /// </summary>
        /// <param name="dbcontext">Contexto da base de dados.</param>
        public JobSchedulerService(NLDbContext dbcontext, INotificacaoService notifService)
        {
            _dbcontext = dbcontext;
            _notifService = notifService;
        }

        /// <summary>
        /// Agenda a verificação do estado de um evento para a sua data de início.
        /// </summary>
        /// <param name="idEvento">ID do evento.</param>
        /// <param name="dataIni">Data de início do evento.</param>
        public void AgendarAtualizacaoEvento(int idEvento, DateTime dataIni)
        {
            BackgroundJob.Schedule(() => AtualizarEstadoEvento(idEvento), dataIni);
            GravarLog($"Evento {idEvento} agendado para atualização na data {dataIni}.");
        }

        /// <summary>
        /// Método chamado pelo Hangfire para atualizar o estado de um evento.
        /// Novo estado pode ser NaoCumpreRequisitosMinimos ou ADecorrer
        /// Manda notificação para o criador sobre o novo estado.
        /// Manda notificação para os participantes caso não cumpra requisitos.
        /// </summary>
        /// <param name="idEvento">ID do evento a ser atualizado.</param>
        public async Task AtualizarEstadoEvento(int idEvento)
        {
            var evento = await _dbcontext.Evento.FindAsync(idEvento);
            if (evento == null || evento.IdEstado != (int)Enums.EstadoEvento.Criado) return;

            var numInscricoes = await _dbcontext.InscricaoEvento
                .Where(i => i.IdEvento == evento.IdEvento)
                .CountAsync();

            var todosItensSelecionados = await _dbcontext.ItemNecessarioEvento
                .Where(i => i.IdEvento == evento.IdEvento)
                .AllAsync(i => i.InscricaoEvento.Any());

            if (numInscricoes < evento.NumMinPessoas || !todosItensSelecionados)
            {
                evento.IdEstado = (int)Enums.EstadoEvento.NaoCumpreRequisitosMinimos;
                // Manda notificação para o criador.
                await _notifService.CriarNotificacao(evento.IdCriador,$"❌ - Evento {evento.Nome} não cumpre os requisitos.");

                // Extrai os IDs dos participantes inscritos no evento
                var idsParticipantes = evento.InscricaoEvento
                    .Select(inscricao => inscricao.IdPessoa)
                    .ToList();

                //Manda notificação para os participantes, se existirem.
                if (idsParticipantes.Any()) {
                    foreach (var id in idsParticipantes)
                    {
                        await _notifService.CriarNotificacao(id, $"❌ - Evento {evento.Nome} foi cancelado.");
                    }
                    
                }

                GravarLog($"[{DateTime.Now}] Evento {evento.IdEvento} - '{evento.Nome}' não cumpre requisitos. Estado alterado para 'Não Cumpre Requisitos'.");
            }
            else
            {
                evento.IdEstado = (int)Enums.EstadoEvento.ADecorrer;

                // Manda notificação para o criador.
                await _notifService.CriarNotificacao(evento.IdCriador, $"🎉 - Evento {evento.Nome} está agora a decorrer.");
                GravarLog($"[{DateTime.Now}] Evento {evento.IdEvento} - '{evento.Nome}' agora está a decorrer. Estado alterado para 'A Decorrer'.");
            }

            _dbcontext.Evento.Update(evento);
            await _dbcontext.SaveChangesAsync();
        }

        /// <summary>
        /// Agenda a verificação do estado de um serviço para a sua data de início.
        /// </summary>
        /// <param name="idServico">ID do serviço.</param>
        /// <param name="dataIni">Data de início do serviço.</param>
        public void AgendarAtualizacaoServico(int idServico, DateTime dataIni)
        {
            BackgroundJob.Schedule(() => AtualizarEstadoServico(idServico), dataIni);
            GravarLog($"Serviço {idServico} agendado para atualização na data {dataIni}.");
        }

        /// <summary>
        /// Método chamado pelo Hangfire para atualizar o estado de um serviço.
        /// Novo estado pode ser NaoCumpreRequisitos ou ADecorrer.
        /// Manda notificação ao criador do novo estado.
        /// </summary>
        /// <param name="idServico">ID do serviço a ser atualizado.</param>
        public async Task AtualizarEstadoServico(int idServico)
        {
            var servico = await _dbcontext.Servico.FindAsync(idServico);
            if (servico == null || servico.IdEstado != (int)Enums.EstadoServico.Criado) return;

            bool temExecutor = await _dbcontext.Servico
                .AnyAsync(s => s.IdServico == servico.IdServico && s.IdExecutor != null);

            if (!temExecutor)
            {
                servico.IdEstado = (int)Enums.EstadoServico.NaoCumpreRequisitosMinimos;
                //Todas as propostas são negadas.
                foreach (var proposta in servico.PropostaServico)
                {
                    proposta.IdEstado = (int)Enums.EstadoProposta.Negada;
                }
                // Manda notificação para o criador.
                await _notifService.CriarNotificacao(servico.IdCriador, $"❌ - Serviço {servico.Nome} não cumpre os requisitos.");
                GravarLog($"[{DateTime.Now}] Serviço {servico.IdServico} - '{servico.Nome}' não cumpre requisitos. Estado alterado para 'Não Cumpre Requisitos'.");
            }
            else
            {
                servico.IdEstado = (int)Enums.EstadoServico.ADecorrer;
                await _notifService.CriarNotificacao(servico.IdCriador, $"🎉 - Serviço {servico.Nome} está agora a decorrer.");
                GravarLog($"[{DateTime.Now}] Serviço {servico.IdServico} - '{servico.Nome}' agora está a decorrer. Estado alterado para 'Aceite'.");
            }

            _dbcontext.Servico.Update(servico);
            await _dbcontext.SaveChangesAsync();
        }

        /// <summary>
        /// Agenda a verificação do estado de um serviço para a sua data de término.
        /// </summary>
        /// <param name="idServico">ID do serviço.</param>
        /// <param name="dataFim">Data de término do serviço.</param>
        public void AgendarTerminoServico(int idServico, DateTime dataFim)
        {
            BackgroundJob.Schedule(() => FinalizarServico(idServico), dataFim);
            GravarLog($"Serviço {idServico} agendado para término na data {dataFim}.");
        }

        /// <summary>
        /// Método chamado pelo Hangfire para finalizar o serviço.
        /// </summary>
        /// <param name="idServico">ID do serviço a ser finalizado.</param>
        public async Task FinalizarServico(int idServico)
        {
            var servico = await _dbcontext.Servico.FindAsync(idServico);
            if (servico == null || servico.IdEstado == (int)Enums.EstadoServico.Concluido) return;

            // Atualiza o estado do serviço para "Concluído" (ou outro estado relevante)
            servico.IdEstado = (int)Enums.EstadoServico.Terminado;
            GravarLog($"Serviço {servico.IdServico} - '{servico.Nome}' foi concluído. Estado alterado para 'Terminado'.");

            _dbcontext.Servico.Update(servico);
            await _dbcontext.SaveChangesAsync();
        }

        /// <summary>
        /// Método auxiliar para registar mensagens de log do HangFire num ficheiro de texto.
        /// </summary>
        /// <param name="message">Mensagem a ser registada.</param>
        private void GravarLog(string message)
        {
            try
            {
                File.AppendAllText(_logFilePath, $"[{DateTime.Now}] "+ message + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao gravar o log: {ex.Message}");
            }
        }
    }
}
