using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sasipca_API.Models;
using Microsoft.AspNetCore.Authorization;
using sasipca_API.Services;
using sasipca_API.Services.Interfaces;
using sasipca_API.Enumerators;
using sasipca_API.Dtos;
using sasipca_API.DBModels;

namespace sasipca_API.Controllers
{
    /// <summary>
    /// Controller para gestão de stocks.
    /// </summary>
    [Route("api/stock")]
    [ApiController]
    [Authorize]
    public class DeliveriesController : ControllerBase
    {
        private readonly SasipcaContext _dbContext;
        private readonly IDeliveryService _deliveryService;
        /// <summary>
        /// Inicialização do DeliveriesController.
        /// Lida com todas as entregas e transições de estado.
        /// </summary>
        public DeliveriesController(SasipcaContext context, IDeliveryService deliveryService)
        {
            _dbContext = context;
            _deliveryService = deliveryService;
        }


        // ----------------------------------------------------
        // ENDPOINT 1: SAÍDA ESPONTÂNEA (ENTREGUE IMEDIATAMENTE)
        // ----------------------------------------------------
        /// <summary>
        /// Regista uma Saída de Stock imediata (entrega espontânea) a um Beneficiário.
        /// Marca o Delivery como 'Entregue' e deduz o stock automáticamente.
        /// </summary>
        [HttpPost("delivery/out")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        public async Task<ActionResult> ImmediateDelivery([FromBody] DeliveryCreationDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // 1. Obter UserId do Context
            var userId = (int)HttpContext.Items["UserId"];

            // 2. Validação da Data (Saída imediata: deve ser hoje ou passado)
            if (dto.ScheduledDate > DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new Resposta("Para uma saída imediata, a ScheduledDate deve ser hoje ou uma data passada."));
            }


            // 3. Chamar o Serviço
            var (success, result) = await _deliveryService.CreateDelivery(
                dto,
                userId,
                Enums.DeliveryStatus.Entregue,
                true // Dedução de Stock: SIM
            );

            if (success)
            {
                return StatusCode(StatusCodes.Status201Created, result);
            }

            // O resultado da falha já contém a Resposta com a mensagem de erro.
            return BadRequest(result);
        }


        // ----------------------------------------------------
        // ENDPOINT 2: AGENDAR SAÍDA (PROGRAMADA)
        // ----------------------------------------------------
        /// <summary>
        /// Agenda uma Entrega futura a um Beneficiário. Não deduz stock; apenas cria o registo.
        /// Marca a Delivery como 'Agendada'.
        /// </summary>
        [HttpPost("delivery/schedule")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        public async Task<ActionResult> ScheduleDelivery([FromBody] DeliveryCreationDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // 1. Obter UserId do Context
            var userIdClaim = HttpContext.Items["UserId"];

            if (userIdClaim == null || !int.TryParse(userIdClaim.ToString(), out int userId))
            {
                return Unauthorized(new Resposta("Utilizador não autenticado ou ID de utilizador inválido."));
            }

            // 2. Validação da Data (Agendamento: deve ser futura)
            if (dto.ScheduledDate < DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new Resposta("Para agendar uma entrega, a ScheduledDate deve ser uma data futura."));
            }

            // 3. Chamar o Serviço
            var (success, result) = await _deliveryService.CreateDelivery(
                dto,
                userId,
                Enums.DeliveryStatus.Agendada, // Status: Agendada (1)
                false // Dedução de Stock: NÃO
            );

            if (success)
            {
                return StatusCode(StatusCodes.Status201Created, result);
            }

            return BadRequest(result);
        }

        // ----------------------------------------------------
        // ENDPOINT 3: ATUALIZAÇÃO E ALTERAÇÃO DE ESTADO DA ENTREGA
        // ----------------------------------------------------
        /// <summary>
        /// Atualiza os dados de uma entrega agendada (data, itens ou status).
        /// Fluxo de estado: Agendada -> [Agendada, Entregue, Cancelada]. Outros estados são finais.
        /// </summary>
        [HttpPut("delivery/{deliveryId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        public async Task<ActionResult> UpdateDelivery(int deliveryId, [FromBody] DeliveryUpdateDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // 1. Obter UserId do Context
            var userId = (int)HttpContext.Items["UserId"];

            // 2. Chamar o Serviço para executar a lógica complexa de atualização/transição de estado
            var (success, result) = await _deliveryService.UpdateDelivery(
                deliveryId,
                dto,
                userId
            );

            if (success)
            {
                return Ok(result);
            }

            // Retorna 400 ou 404 dependendo do erro retornado pelo serviço.
            if (result.Message.Contains("não encontrado"))
            {
                return NotFound(result);
            }

            return BadRequest(result);
        }


        // ----------------------------------------------------
        // ENDPOINT 4: CONSULTA DE ENTREGAS (COM FILTROS)
        // ----------------------------------------------------
        /// <summary>
        /// Retorna a lista de todas as entregas (cabeçalhos), com opções de filtragem por status, beneficiário e data.
        /// </summary>
        /// <param name="query">Parâmetros de filtro (StatusId, BeneficiaryId, DateFrom, DateTo).</param>
        /// <returns>Lista de cabeçalhos de entregas.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<VDelivery>))]
        public async Task<ActionResult<IEnumerable<VDelivery>>> GetDeliveries([FromQuery] DeliveryQueryDTO query)
        {
            // Usamos a View pré-agregada que já contém todos os nomes e o status em formato string.
            var deliveriesQuery = _dbContext.VDeliveries.AsQueryable();

            // 1. Aplicação dos Filtros

            if (query.StatusId.HasValue)
            {
                // NOTA: Se o StatusId for passado, precisamos de filtrar pela string correspondente.
                // É melhor ter uma coluna StatusId na View ou mapear o StatusId para a string do enum:
                var statusString = ((Enums.DeliveryStatus)query.StatusId.Value).ToString();
                deliveriesQuery = deliveriesQuery.Where(d => d.Status == statusString);

                // ALTERNATIVA: Se quiser manter o StatusId como int na query:
                // deliveriesQuery = deliveriesQuery.Where(d => d.StatusId == (int)query.StatusId.Value); 
                // Mas a View teria de incluir a coluna StatusId.
            }

            if (query.BeneficiaryId.HasValue)
            {
                deliveriesQuery = deliveriesQuery.Where(d => d.BeneficiaryId == query.BeneficiaryId.Value);
            }

            if (query.DateFrom.HasValue)
            {
                deliveriesQuery = deliveriesQuery.Where(d => d.ScheduledDate >= DateOnly.FromDateTime(query.DateFrom.Value));
            }

            if (query.DateTo.HasValue)
            {
                deliveriesQuery = deliveriesQuery.Where(d => d.ScheduledDate <= DateOnly.FromDateTime(query.DateTo.Value));
            }

            // 2. Execução da Query
            var result = await deliveriesQuery
                .OrderByDescending(d => d.ScheduledDate)
                .ToListAsync();

            return Ok(result);
        }
    }

}