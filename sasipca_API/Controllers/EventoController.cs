using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sasipca_API.Models;
using Microsoft.AspNetCore.Authorization;
using sasipca_API.Enumerators;
using sasipca_API.Services.Interfaces;
using sasipca_API.Data;
using sasipca_API.Dtos;

namespace sasipca_API.Controllers
{
    /// <summary>
    /// Controller para gestão de eventos
    /// </summary>
    [Route("api/evento")]
    [ApiController]
    [Authorize]
    public class EventoController : ControllerBase
    {
        private readonly NLDbContext _dbcontext;
        private readonly INotificacaoService _notifService;
        private readonly IAuthService _authService;
        private readonly IJobSchedulerService _jobSchedulerService;

        /// <summary>
        /// Inicialização do EventoController
        /// </summary>
        /// <param name="context">Contexto da base de dados</param>
        /// <param name="authService">Serviço de autenticação</param>
        /// <param name="jobSchedulerService">Serviço de agendamento de tarefas</param>
       
        public EventoController(NLDbContext context, INotificacaoService notifService, IAuthService authService, IJobSchedulerService jobSchedulerService)
        {
            _dbcontext = context;
            _notifService = notifService;
            _authService = authService;
            _jobSchedulerService = jobSchedulerService;
        }

        /// <summary>
        /// Retorna os detalhes de um evento pelo ID
        /// </summary>
        /// <remarks>
        /// Apenas as pessoas que partilham o mesmo código postal que o evento podem aceder.
        /// 
        /// </remarks>
        /// <param name="eventoId">ID do evento</param>
        /// <returns>Detalhes do evento</returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EventoGetDTO))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [Produces("application/json")]
        [HttpGet("{eventoId}")]
        public async Task<ActionResult<EventoGetDTO>> GetEventoDetalhes(int eventoId)
        {
            try
            {
                var userId = (int)HttpContext.Items["UserId"];
                string? codPostalPessoaAutenticada = "5555";
                bool isCriador = false;

            //Busca evento, por ID.
            var evento = await _dbcontext.Evento
                .Include(e => e.IdCriadorNavigation)
                .Include(e => e.ItemNecessarioEvento)
                .ThenInclude(item => item.InscricaoEvento)
                .FirstOrDefaultAsync(e => e.IdEvento == eventoId);

                if (evento == null)
                    return NotFound(new Resposta("Evento não encontrado."));

                if (evento.IdCriadorNavigation.IdCodPostal != codPostalPessoaAutenticada)
                    return Unauthorized(new Resposta("Você não tem acesso a este recurso."));

                if (evento.IdCriador == userId) isCriador = true;

                var eventoDTO = new EventoGetDTO
                {
                    IdEvento = evento.IdEvento,
                    Criador = new PessoaSimpleDTO
                    {
                        Id = evento.IdCriador,
                        Nome = evento.IdCriadorNavigation.Nome
                    },
                    IsCriador = isCriador,
                    IdEstado = evento.IdEstado,
                    Nome = evento.Nome,
                    Morada = evento.Morada,
                    NumMinPessoas = evento.NumMinPessoas,
                    Descricao = evento.Descricao,
                    DataCriacao = evento.DataCriacao,
                    DataIni = evento.DataIni,
                    ItensNecessarios = evento.ItemNecessarioEvento.Select(item => new ItemNecessarioGetDTO
                    {
                        IdItem = item.IdItem,
                        Nome = item.Nome,
                        Quantidade = item.Quantidade ?? 1,
                        isSelecionado = item.InscricaoEvento.Any()
                    }).ToList()
                };

                return Ok(eventoDTO);
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao obter os detalhes do evento."));
            }
        }
        /// <summary>
        /// Retorna as inscricoes feitas a um evento pelo ID de Evento
        /// </summary>
        /// <remarks>
        /// Apenas o criador tem acesso a esta informação.
        /// </remarks>
        /// <param name="eventoId">ID do evento</param>
        /// <returns>Inscrições do evento</returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<InscricaoEventoDTO>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [Produces("application/json")]
        [HttpGet("{eventoId}/inscricoes")]
        public async Task<ActionResult<IEnumerable<InscricaoEventoDTO>>> GetInscricoesEvento(int eventoId)
        {
            try
            {
                var userId = (int)HttpContext.Items["UserId"];

                var evento = await _dbcontext.Evento
                    .Include(e => e.IdCriadorNavigation)
                    .Include(e => e.ItemNecessarioEvento)
                        .ThenInclude(item => item.InscricaoEvento)
                            .ThenInclude(insc => insc.IdPessoaNavigation)
                    .FirstOrDefaultAsync(e => e.IdEvento == eventoId);

                if (evento == null)
                    return NotFound(new Resposta("Evento não encontrado."));

                if (evento.IdCriadorNavigation.IdPessoa != userId)
                    return Unauthorized(new Resposta("Você não tem acesso a este recurso."));

                // Obter todas as inscrições do evento
                var inscricoes = evento.ItemNecessarioEvento
                    .SelectMany(item => item.InscricaoEvento)
                    .Select(insc => new InscricaoEventoDTO
                    {
                        IdInscricao = insc.IdInscricao,
                        IdPessoa = insc.IdPessoa,
                        NomePessoa = insc.IdPessoaNavigation.Nome,
                        DataInscricao = insc.DataInscricao,
                        Item = new ItemInscricaoDTO
                        {
                            IdItem = insc.IdItem,
                            Nome = insc.IdItemNavigation.Nome,
                            Quantidade = insc.IdItemNavigation.Quantidade??1
                        }
                    })
                    .ToList();

                return Ok(inscricoes);
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao obter as inscricoes do evento."));
            }
        }

        /// <summary>
        /// Criar um novo anúncio de evento
        /// </summary>
        /// <remarks>
        /// Ao criar um evento, é definida uma tarefa para a data/hora de início do evento que irá verificar os requisitos de início do evento.
        /// 
        /// </remarks>
        /// <param name="eventoDTO">Dados do novo evento</param>
        /// <returns>Mensagem de confirmação</returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [Produces("application/json")]
        [HttpPost]
        public async Task<ActionResult<Resposta>> PostEvento([FromBody] EventoPostDTO eventoDTO)
        {
            try
            {
                var userId = (int)HttpContext.Items["UserId"];

                var evento = new Evento
                {
                    Nome = eventoDTO.Nome,
                    Morada = eventoDTO.Morada,
                    DataIni = eventoDTO.DataIni,
                    NumMinPessoas = eventoDTO.NumMinPessoas,
                    Descricao = eventoDTO.Descricao,
                    IdEstado = (int)Enums.EstadoEvento.Criado,
                    IdCriador = userId
                };

                _dbcontext.Evento.Add(evento);
                await _dbcontext.SaveChangesAsync();

                _dbcontext.ItemNecessarioEvento.AddRange(
                    eventoDTO.ItensNecessarios.Select(i => new ItemNecessarioEvento
                    {
                        IdEvento = evento.IdEvento,
                        Nome = i.Nome,
                        Quantidade = i.Quantidade
                    })
                );
                await _dbcontext.SaveChangesAsync();

                _jobSchedulerService.AgendarAtualizacaoEvento(evento.IdEvento, evento.DataIni);

                return Ok(new Resposta("Evento criado com sucesso."));
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao criar o evento."));
            }
        }

        /// <summary>
        /// Cancelar um evento
        /// </summary>
        /// <remarks>
        /// Apenas é possível cancelar um evento se ele estiver no estado 'Criado'.
        /// Apenas o criador pode cancelar o evento.
        /// </remarks>
        /// <param name="eventoId">ID do evento a cancelar</param>
        /// <returns>Mensagem de confirmação</returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [Produces("application/json")]
        [HttpPut("{eventoId}/cancelar")]
        public async Task<ActionResult<Resposta>> CancelarEvento(int eventoId)
        {
            return await AlterarEstadoEvento(eventoId, Enums.EstadoEvento.Cancelado);
        }

        /// <summary>
        /// Concluir um evento
        /// </summary>
        /// <remarks>
        /// Apenas é possível concluir um evento se estiver no estado 'A Decorrer'.
        /// Apenas o criador pode concluir o evento.
        /// </remarks>
        /// <param name="eventoId">ID do evento a concluir</param>
        /// <returns>Mensagem de confirmação</returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [Produces("application/json")]
        [HttpPut("{eventoId}/terminar")]
        public async Task<ActionResult<Resposta>> TerminarEvento(int eventoId)
        {
            return await AlterarEstadoEvento(eventoId, Enums.EstadoEvento.Concluido);
        }

        /// <summary>
        /// Inscrever-se num evento
        /// </summary>
        /// <remarks>
        /// Permite que um utilizador se inscreva num evento criado.
        /// O itemId é opcional - apenas necessário se o evento tiver itens necessários.
        /// 
        /// </remarks>
        /// <param name="eventoId">ID do evento</param>
        /// <param name="itemId">ID do item selecionado (opcional)</param>
        /// <returns>Mensagem de confirmação</returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [Produces("application/json")]
        [HttpPost("{eventoId}/inscrever")]
        public async Task<ActionResult<Resposta>> InscreverEvento(int eventoId, int? itemId = null)
        {
            try
            {
                var userId = (int)HttpContext.Items["UserId"];
                string? codPostalPessoaAutenticada = "5555";

                //Busca nome da pessoa autenticada (para a notificação)
                string? nomePessoaAutenticada = await _authService.ObterNome(userId);

                // Busca evento por ID e verifica se existe
                var evento = await _dbcontext.Evento
                    .Include(e => e.InscricaoEvento)
                    .Include(e => e.IdCriadorNavigation)
                    .Include(e => e.ItemNecessarioEvento)
                    .FirstOrDefaultAsync(e => e.IdEvento == eventoId);

                if (evento == null)
                    return NotFound(new Resposta("Evento não encontrado."));

                if (evento.IdCriadorNavigation.IdCodPostal != codPostalPessoaAutenticada)
                    return Unauthorized(new Resposta("Você não pode se inscrever neste evento."));

                if (evento.InscricaoEvento.Any(i => i.IdPessoa == userId))
                    return BadRequest(new Resposta("Você já está inscrito neste evento."));

                // Se o evento tem itens necessários
                if (evento.ItemNecessarioEvento.Any())
                {
                    if (!itemId.HasValue)
                        return BadRequest(new Resposta("Este evento requer a seleção de um item."));

                    var item = await _dbcontext.ItemNecessarioEvento
                        .FirstOrDefaultAsync(i => i.IdItem == itemId.Value && i.IdEvento == eventoId);

                    if (item == null)
                        return BadRequest(new Resposta("Item selecionado não é válido para este evento."));

                    if (evento.InscricaoEvento.Any(i => i.IdItem == itemId.Value))
                        return BadRequest(new Resposta("Este item já foi escolhido por outra pessoa."));
                }

                var inscricao = new InscricaoEvento
                {
                    IdEvento = eventoId,
                    IdPessoa = userId,
                    IdItem = itemId
                };

                _dbcontext.InscricaoEvento.Add(inscricao);
                await _dbcontext.SaveChangesAsync();

                // Envia notificação usando o serviço injetado
                await _notifService.CriarNotificacao(
                    evento.IdCriador,
                    $"✅ - {nomePessoaAutenticada} inscreveu-se no seu evento \"{evento.Nome}\"."
                );

                return Ok(new Resposta("Inscrição realizada com sucesso."));
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao processar a inscrição."));
            }
        }

        /// <summary>
        /// Apagar uma inscrição num evento
        /// </summary>
        /// <remarks>
        /// Permite que o criador do evento apague uma inscrição.
        /// O utilizador deve selecionar uma inscrição válida para o evento.
        /// 
        /// Exemplo de requisição:
        /// DELETE /api/evento/1/1
        /// </remarks>
        /// <param name="eventoId">ID do evento</param>
        /// <param name="inscricaoId">ID da inscrição a apagar</param>
        /// <returns>Mensagem de confirmação</returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [Produces("application/json")]
        [HttpDelete("{eventoId}/{inscricaoId}")]
        public async Task<ActionResult<Resposta>> DeleteInscricao(int eventoId, int inscricaoId)
        {
            try
            {
                var userId = (int)HttpContext.Items["UserId"];

                // Verificar se o evento existe e obter o criador
                var evento = await _dbcontext.Evento
                    .FirstOrDefaultAsync(e => e.IdEvento == eventoId);

                if (evento == null)
                    return NotFound(new Resposta("Evento não encontrado."));

                // Verificar se o utilizador é o criador do evento
                if (evento.IdCriador != userId)
                    return Unauthorized(new Resposta("Você não tem acesso a este recurso."));

                if (evento.IdEvento != (int)Enums.EstadoEvento.Criado)
                    return Unauthorized(new Resposta("Não é possível remover uma inscrição neste momento."));

                // Verificar se a inscrição existe e pertence ao evento
                var inscricao = await _dbcontext.InscricaoEvento
                    .FirstOrDefaultAsync(i => i.IdInscricao == inscricaoId && i.IdEvento == eventoId);

                if (inscricao == null)
                    return NotFound(new Resposta("Inscrição não encontrada para este evento."));

                // Remover a inscrição
                _dbcontext.InscricaoEvento.Remove(inscricao);
                await _dbcontext.SaveChangesAsync();

                return Ok(new Resposta("Inscrição removida com sucesso."));
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao remover a inscrição."));
            }
            
        }

        /// <summary>
        /// Método auxiliar para alterar o estado de um evento
        /// </summary>
        private async Task<ActionResult<Resposta>> AlterarEstadoEvento(int eventoId, Enums.EstadoEvento novoEstado)
        {
            try
            {
                var userId = (int)HttpContext.Items["UserId"];
                var evento = await _dbcontext.Evento.FindAsync(eventoId);

                if (evento == null)
                    return NotFound(new Resposta("Evento não encontrado."));

                if (evento.IdCriador != userId)
                    return Unauthorized(new Resposta("Você não tem permissão para esta ação."));

                if (novoEstado == Enums.EstadoEvento.Cancelado && evento.IdEstado != (int)Enums.EstadoEvento.Criado)
                    return BadRequest(new Resposta("Não é possível cancelar um evento que esteja de momento a decorrer."));
                else
                {
                    //Extrai os IDs dos participantes inscritos no evento
                    var idsParticipantes = evento.InscricaoEvento
                        .Select(inscricao => inscricao.IdPessoa)
                        .ToList();

                    //Manda notificação para os participantes, se existirem.
                    if (idsParticipantes.Any())
                    {
                        foreach (var id in idsParticipantes)
                        {
                            await _notifService.CriarNotificacao(id, $"❌ - Evento {evento.Nome} foi cancelado.");
                        }

                    }
                }

                if (novoEstado == Enums.EstadoEvento.Concluido && evento.IdEstado != (int)Enums.EstadoEvento.ADecorrer)
                    return BadRequest(new Resposta("O evento só pode ser terminado se estiver a decorrer."));

                evento.IdEstado = (int)novoEstado;
                await _dbcontext.SaveChangesAsync();

                return Ok(new Resposta("Ação realizada com sucesso."));
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao alterar o estado do evento."));
            }
        }
    }
}