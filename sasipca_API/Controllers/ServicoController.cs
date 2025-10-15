using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sasipca_API.Models;
using Microsoft.AspNetCore.Authorization;
using sasipca_API.Services;
using sasipca_API.Enumerators;
using sasipca_API.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using sasipca_API.Dtos;

namespace sasipca_API.Controllers
{
    /// <summary>
    /// Controller para gestão de serviços.
    /// </summary>
    [Route("api/servico")]
    [ApiController]
    [Authorize]
    public class ServicoController : ControllerBase
    {
        private readonly NLDbContext _dbcontext;
        private readonly NotificacaoService _notifService;
        private readonly AuthService _authService;
        private readonly AzureStorageService _storageService;
        private readonly ImageProcessingService _imageProcessingService;
        private readonly JobSchedulerService _jobSchedulerService;

        /// <summary>
        /// Inicialização do ServicoController
        /// </summary>
        public ServicoController(NLDbContext context, NotificacaoService notifService,AuthService authService, AzureStorageService storageService, ImageProcessingService imageProcessingService, JobSchedulerService jobSchedulerService)
        {
            _dbcontext = context;
            _notifService = notifService;
            _authService = authService;
            _storageService = storageService;
            _imageProcessingService = imageProcessingService;
            _jobSchedulerService = jobSchedulerService;
        }

        /// <summary>
        /// Busca os detalhes de um serviço
        /// </summary>
        /// <remarks>
        /// Para buscar um serviço, o utilizador autenticado deve partilhar o mesmo código-postal com o criador do serviço.
        /// 
        /// </remarks>
        /// <param name="servicoId">ID do serviço</param>
        /// <returns>Detalhes do serviço</returns>
        [HttpGet("{servicoId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ServicoGetDTO))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [Produces("application/json")]
        public async Task<ActionResult<Resposta>> GetServico(int servicoId)
        {
            try
            {
                var userId = (int)HttpContext.Items["UserId"];
                bool isCriador = false;

                string? codPostalPessoaAutenticada = "5555";

                var servico = await _dbcontext.Servico
                    .Include(s => s.IdCriadorNavigation)
                    .Include(s => s.IdEstadoNavigation)
                    .Include(s => s.IdModalidadeprecoNavigation)
                    .Include(s => s.IdImagem)
                    .FirstOrDefaultAsync(s => s.IdServico == servicoId);

                if (servico == null)
                    return NotFound(new Resposta("Serviço não encontrado."));

                if (servico.IdCriadorNavigation.IdCodPostal != codPostalPessoaAutenticada)
                    return Unauthorized(new Resposta("Você não tem permissão para aceder a este recurso."));

                if (servico.IdCriador == userId) isCriador = true;

                var servicoDTO = new ServicoGetDTO
                {
                    IdServico = servico.IdServico,
                    IsCriador = isCriador,
                    IdEstado = servico.IdEstado,
                    Nome = servico.Nome,
                    Descricao = servico.Descricao,
                    DataIni = servico.DataIni,
                    DataFim = servico.DataFim,
                    DataCriacao = servico.DataCriacao,
                    Preco = servico.Preco,
                    Criador = new PessoaSimpleDTO
                    {
                        Id = servico.IdCriador,
                        Nome = servico.IdCriadorNavigation.Nome
                    },
                    ModalidadePreco = servico.IdModalidadeprecoNavigation.Tipo,
                    Imagens = servico.IdImagem.Select(i => i.Url).ToList()
                };

                return Ok(servicoDTO);
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao obter o serviço."));
            }
        }

        /// <summary>
        /// Criar um novo Serviço
        /// </summary>
        /// <remarks>
        /// Máximo de upload de imagens: 4.
        /// Ao criar um serviço, é definida uma tarefa para a data/hora de início do serviço que irá verificar os requisitos de início do serviço.
        /// Se este tiver Data/Hora de fim de serviço, é definida uma tarefa para essa hora para terminar automáticamente o serviço.
        /// 
        /// Exemplo de requisição:
        /// FormData:
        /// - Nome: string
        /// - Descricao: string
        /// - DataIni: DateTime
        /// - DataFim: DateTime (opcional)
        /// - Preco: decimal
        /// - IdModalidadePreco: int
        /// - Imagens: ficheiros de imagem (máx 4, opcional)
        /// </remarks>
        /// <param name="servicoPostDTO">Dados do serviço</param>
        /// <returns>Mensagem de confirmação</returns>
        [HttpPost]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        public async Task<ActionResult<Resposta>> PostServico([FromForm] ServicoPostDTO servicoPostDTO)
        {
            try
            {
                var userId = (int)HttpContext.Items["UserId"];

                if (servicoPostDTO.Imagens != null && servicoPostDTO.Imagens.Count > 4)
                {
                    return BadRequest(new Resposta("Máximo de imagens inseridas excedido (4)."));
                }

                var servico = new Servico
                {
                    Nome = servicoPostDTO.Nome,
                    Descricao = servicoPostDTO.Descricao,
                    DataIni = servicoPostDTO.DataIni,
                    DataFim = servicoPostDTO.DataFim,
                    Preco = servicoPostDTO.Preco,
                    IdModalidadepreco = servicoPostDTO.IdModalidadePreco,
                    IdCriador = userId,
                    IdEstado = (int)Enums.EstadoServico.Criado
                };

                _dbcontext.Servico.Add(servico);
                await _dbcontext.SaveChangesAsync();

                if (servicoPostDTO.Imagens != null && servicoPostDTO.Imagens.Count > 0 && servicoPostDTO.Imagens.Count < 4)
                {
                    var imagens = await _imageProcessingService.ProcessarImagens(servicoPostDTO.Imagens);
                    servico.IdImagem = imagens;
                    await _dbcontext.SaveChangesAsync();
                }

                _jobSchedulerService.AgendarAtualizacaoServico(servico.IdServico, servico.DataIni);

                if (servicoPostDTO.DataFim is not null)
                    _jobSchedulerService.AgendarTerminoServico(servico.IdServico, servico.DataFim.Value);

                return Ok(new Resposta("Serviço criado com sucesso!"));
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao criar o serviço."));
            }
        }

        /// <summary>
        /// Busca as modalidades de preço para serviços
        /// </summary>
        /// <remarks>
        /// Não necessita de autenticação para aceder
        /// 
        /// </remarks>
        /// <returns>Lista de modalidades de preço</returns>
        [HttpGet("modalidadepreco")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ModalidadeGetDTO>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [Produces("application/json")]
        public async Task<ActionResult<Resposta>> GetModalidadePreco()
        {
            try
            {
                var modalidadePreco = await _dbcontext.ModalidadePreco
                    .Select(c => new ModalidadeGetDTO
                    {
                        IdModalidade = c.IdModalidade,
                        Nome = c.Tipo
                    })
                    .ToListAsync();

                if (modalidadePreco == null || !modalidadePreco.Any())
                    return NotFound(new Resposta("Nenhuma modalidade de preço encontrada."));

                return Ok(modalidadePreco);
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao obter as modalidades de preço."));
            }
        }

        /// <summary>
        /// Criar uma proposta para um serviço
        /// </summary>
        /// <remarks>
        /// Cada utilizador apenas pode criar uma proposta para um serviço.
        /// O Criador do serviço não pode criar uma proposta.
        /// O utilizador necessita de ter o mesmo código postal do criador do serviço.
        /// Envia uma notificação para o criador do serviço que foi criada uma nova proposta.
        /// </remarks>
        /// <param name="servicoId">ID do serviço</param>
        /// <returns>Mensagem de confirmação</returns>
        [HttpPost("{servicoId}/propor")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        public async Task<ActionResult<Resposta>> PostPropostaServico([FromRoute] int servicoId)
        {
                var userId = (int)HttpContext.Items["UserId"];

                var servico = await _dbcontext.Servico
                    .Include(p => p.IdCriadorNavigation)
                    .FirstOrDefaultAsync(p => p.IdServico == servicoId);

                if (servico == null)
                    return NotFound(new Resposta("Serviço não encontrado."));

                if (servico.IdCriador == userId)
                    return BadRequest(new Resposta("O criador do serviço não pode fazer uma proposta a si mesmo."));

                var propostaExistente = await _dbcontext.PropostaServico
                    .FirstOrDefaultAsync(p => p.IdServico == servicoId && p.IdExecutor == userId);

                if (propostaExistente != null)
                    return BadRequest(new Resposta("Você não pode criar outra proposta para este serviço."));

                if (servico.IdCriadorNavigation.IdCodPostal != "5555")
                    return BadRequest(new Resposta("Você não tem acesso a este recurso."));

                var propostaServico = new PropostaServico
                {
                    IdExecutor = userId,
                    IdServico = servicoId,
                    IdEstado = (int)Enums.EstadoProposta.Criada
                };

                // Guarda dados.
                _dbcontext.PropostaServico.Add(propostaServico);
                await _dbcontext.SaveChangesAsync();

                //Envia uma notificação para o criador do serviço
                await _notifService.CriarNotificacao(servico.IdCriador,
                    $"✅ - Recebeu uma nova proposta para o serviço \"{servico.Nome}\".");


                return Ok(new {message = "Proposta criada com sucesso."});
           
		}

        /// <summary>
        /// Buscar todas as propostas de um serviço
        /// </summary>
        /// <remarks>
        /// Apenas o utilizador criador do serviço tem acesso.
        /// Busca propostas no estado 'Criada' ou 'Standby'
        /// 
        /// </remarks>
        /// <param name="servicoId">ID do serviço</param>
        /// <returns>Lista de propostas</returns>
        [HttpGet("{servicoId}/propostas")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<PropostaServicoGetDTO>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        public async Task<ActionResult<Resposta>> GetPropostasServico([FromRoute] int servicoId)
        {
            try
            {
                var userId = (int)HttpContext.Items["UserId"];

                var servico = await _dbcontext.Servico.FindAsync(servicoId);
                if (servico == null)
                    return NotFound(new Resposta("Serviço não encontrado."));

                if (servico.IdCriador != userId)
                    return Unauthorized(new Resposta("Você não tem acesso a este recurso."));

                var propostas = await _dbcontext.PropostaServico
                    .Where(p => p.IdServico == servicoId &&
                           (p.IdEstado == (int)Enums.EstadoProposta.Criada ||
                            p.IdEstado == (int)Enums.EstadoProposta.Standby))
                    .Select(p => new PropostaServicoGetDTO
                    {
                        IdPropostaServico = p.IdPropostaServico,
                        Executor = new PessoaSimpleDTO
                        {
                            Id = p.IdExecutor,
                            Nome = p.IdExecutorNavigation.Nome,
                            Contacto = p.IdExecutorNavigation.Contacto
                        }
                    })
                    .ToListAsync();

                return Ok(propostas);
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao obter as propostas."));
            }
        

        }


        /// <summary>
        /// Cancelar um serviço
        /// </summary>
        /// <remarks>
        /// Apenas é possível cancelar um serviço se ele estiver no estado 'Criado'.
        /// Apenas o criador consegue cancelar o serviço.
        /// 
        /// Exemplo de resposta:
        /// {
        ///     "message": "Serviço cancelado com sucesso"
        /// }
        /// </remarks>
        /// <param name="servicoId">ID do serviço</param>
        /// <returns>Mensagem de confirmação</returns>
        [HttpPut("{servicoId}/cancelar")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [Produces("application/json")]
        public async Task<ActionResult<Resposta>> CancelarServico(int servicoId)
        {
            try
            {
                return await AlterarEstadoServico(servicoId, Enums.EstadoServico.Cancelado);
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao cancelar o serviço."));
            }
        }

        /// <summary>
        /// Terminar um serviço
        /// </summary>
        /// <remarks>
        /// Apenas é possível terminar um serviço se estiver no estado 'A Decorrer'.
        /// 
        /// Exemplo de resposta:
        /// {
        ///     "message": "Serviço terminado com sucesso"
        /// }
        /// </remarks>
        /// <param name="servicoId">ID do serviço</param>
        /// <returns>Mensagem de confirmação</returns>
        [HttpPut("{servicoId}/terminar")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [Produces("application/json")]
        public async Task<ActionResult<Resposta>> TerminarServico(int servicoId)
        {
            try
            {
                return await AlterarEstadoServico(servicoId, Enums.EstadoServico.Terminado);
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao terminar o serviço."));
            }
        }

        /// <summary>
        /// Aceitar uma proposta para um serviço
        /// </summary>
        /// <remarks>
        /// Apenas o utilizador criador do serviço tem acesso.
        /// A proposta selecionada será marcada como 'Aceite', e todas as outras serão 'Standby'.
        /// 
        /// Exemplo de resposta:
        /// {
        ///     "message": "Proposta aceite com sucesso"
        /// }
        /// </remarks>
        /// <param name="servicoId">ID do serviço</param>
        /// <param name="propostaId">ID da proposta</param>
        /// <returns>Mensagem de confirmação</returns>
        [HttpPut("{servicoId}/aceitarproposta")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        public async Task<ActionResult<Resposta>> AceitarProposta([FromRoute] int servicoId, [FromQuery] int propostaId)
        {
            try
            {
                var userId = (int)HttpContext.Items["UserId"];

                var servico = await _dbcontext.Servico.FindAsync(servicoId);
                if (servico == null)
                    return NotFound(new Resposta("Serviço não encontrado."));

                if (servico.IdCriador != userId)
                    return Unauthorized(new Resposta("Você não tem acesso a este recurso."));

                if (servico.IdEstado != (int)Enums.EstadoServico.Criado)
                {
                    return Unauthorized(new Resposta("Não é possível aceitar uma proposta para este serviço neste momento."));
                }

                var propostaSelecionada = await _dbcontext.PropostaServico.FindAsync(propostaId);
                if (propostaSelecionada == null || propostaSelecionada.IdServico != servicoId)
                    return NotFound(new Resposta("Proposta não encontrada."));

                propostaSelecionada.IdEstado = (int)Enums.EstadoProposta.Aceite;

                var outrasPropostas = await _dbcontext.PropostaServico
                    .Where(p => p.IdServico == servicoId && p.IdPropostaServico != propostaId)
                    .ToListAsync();

                foreach (var proposta in outrasPropostas)
                {
                    proposta.IdEstado = (int)Enums.EstadoProposta.Standby;
                }

                servico.IdEstado = (int)Enums.EstadoServico.Aceite;
                await _dbcontext.SaveChangesAsync();

                //Envia uma notificação para o criador da proposta
                await _notifService.CriarNotificacao(propostaSelecionada.IdExecutor,
                    $"✅ - A sua proposta para o serviço \"{servico.Nome}\" foi aceite.");

                return Ok(new Resposta("Proposta aceite com sucesso."));
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao aceitar a proposta."));
            }
        }

        /// <summary>
        /// Remover uma proposta para um serviço
        /// </summary>
        /// <remarks>
        /// Apenas o utilizador criador do serviço tem acesso.
        /// Todas as propostas são marcadas como 'Standby'.
        /// O estado do serviço voltará a estado 'Criado'.
        /// 
        /// Exemplo de resposta:
        /// {
        ///     "message": "Proposta removida com sucesso"
        /// }
        /// </remarks>
        /// <param name="servicoId">ID do serviço</param>
        /// <returns>Mensagem de confirmação</returns>
        [HttpPut("{servicoId}/removerproposta")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        public async Task<ActionResult<Resposta>> RemoverProposta([FromRoute] int servicoId)
        {
            try
            {
                var userId = (int)HttpContext.Items["UserId"];

                var servico = await _dbcontext.Servico.FindAsync(servicoId);
                if (servico == null)
                    return NotFound(new Resposta("Serviço não encontrado."));

                if (servico.IdCriador != userId)
                    return Unauthorized(new Resposta("Você não tem acesso a este recurso."));

                if (servico.IdEstado != (int)Enums.EstadoServico.Aceite)
                {
                    return Unauthorized(new Resposta("Não é possível remover uma proposta para este serviço neste momento."));
                }

                var propostaSelecionada = await _dbcontext.PropostaServico
                    .Where(p => p.IdServico == servico.IdServico && p.IdEstado == (int)Enums.EstadoProposta.Aceite)
                    .ToListAsync();

                if (propostaSelecionada == null)
                    return NotFound(new Resposta("Proposta aceite não encontrada."));

                var todasPropostas = await _dbcontext.PropostaServico
                    .Where(p => p.IdServico == servicoId)
                    .ToListAsync();

                foreach (var proposta in todasPropostas)
                {
                    proposta.IdEstado = (int)Enums.EstadoProposta.Standby;
                }

                servico.IdEstado = (int)Enums.EstadoServico.Criado;
                await _dbcontext.SaveChangesAsync();

                //Envia uma notificação para o criador da proposta
                await _notifService.CriarNotificacao(propostaSelecionada.First().IdExecutor,
                    $"❌ - A sua proposta para o serviço \"{servico.Nome}\" foi removida.");

                return Ok(new Resposta("Proposta removida com sucesso."));
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao remover a proposta."));
            }
        }

        /// <summary>
        /// Avaliar um serviço
        /// </summary>
        /// <remarks>
        /// Apenas é possível avaliar um serviço se ele estiver no estado 'Terminado'.
        /// Apenas o criador do serviço pode avaliar.
        /// Após avaliação o serviço é dado como 'Concluído'.
        /// 
        /// </remarks>
        /// <param name="servicoId">ID do serviço</param>
        /// <param name="nota">Nota de 1 a 5</param>
        /// <returns>Mensagem de confirmação</returns>
        [HttpPost("{servicoId}/avaliar")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        public async Task<ActionResult<Resposta>> AvaliarServico([FromRoute] int servicoId, [FromQuery] int nota)
        {
            try
            {
                var userId = (int)HttpContext.Items["UserId"];

                var servico = await _dbcontext.Servico.FindAsync(servicoId);
                if (servico == null)
                    return NotFound(new Resposta("Serviço não encontrado."));

                if (servico.IdCriador != userId)
                    return Unauthorized(new Resposta("Você não tem permissão para avaliar este serviço."));

                if (servico.IdEstado != (int)Enums.EstadoServico.Terminado)
                {
                    return BadRequest(new Resposta("Só é possível avaliar um serviço que esteja no estado 'Terminado'."));
                }

                if (nota < 1 || nota > 5)
                {
                    return BadRequest(new Resposta("A nota deve estar entre 1 e 5."));
                }

                var avaliacao = new Avaliacao
                {
                    IdUtilizador = userId,
                    Nota = nota
                };

                _dbcontext.Avaliacao.Add(avaliacao);
                servico.IdEstado = (int)Enums.EstadoServico.Concluido;
                await _dbcontext.SaveChangesAsync();

                return Ok(new Resposta("Avaliação realizada com sucesso."));
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao avaliar o serviço."));
            }
        }

        /// <summary>
        /// Método interno para alterar estado do serviço
        /// </summary>
        private async Task<ActionResult<Resposta>> AlterarEstadoServico(int servicoId, Enums.EstadoServico novoEstado)
        {
            var userId = (int)HttpContext.Items["UserId"];

            var servico = await _dbcontext.Servico
                .Include(s => s.PropostaServico)
                .FirstOrDefaultAsync(s => s.IdServico == servicoId);

            if (servico == null)
                return NotFound(new Resposta("Serviço não encontrado."));

            if (servico.IdCriador != userId)
                return Unauthorized(new Resposta("Você não tem acesso a este recurso."));

            switch (novoEstado)
            {
                case Enums.EstadoServico.Cancelado when servico.IdEstado != (int)Enums.EstadoServico.Criado:
                    return BadRequest(new Resposta("Não é possível cancelar um serviço que já esteja a decorrer."));

                case Enums.EstadoServico.Terminado when servico.IdEstado != (int)Enums.EstadoServico.ADecorrer:
                    return BadRequest(new Resposta("O serviço só pode ser terminado se estiver a decorrer."));
            }

            servico.IdEstado = (int)novoEstado;

            if (novoEstado == Enums.EstadoServico.Cancelado)
            {
                foreach (var proposta in servico.PropostaServico)
                {
                    proposta.IdEstado = (int)Enums.EstadoProposta.Negada;
                }
            }
            else if (novoEstado == Enums.EstadoServico.Terminado)
            {
                foreach (var proposta in servico.PropostaServico.Where(p => p.IdEstado == (int)Enums.EstadoProposta.Aceite))
                {
                    proposta.IdEstado = (int)Enums.EstadoProposta.Negada;
                }
            }

            await _dbcontext.SaveChangesAsync();

            return Ok(new Resposta("Ação realizada com sucesso."));
        }
    }
}