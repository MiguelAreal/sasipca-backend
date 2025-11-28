using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sasipca_API.DBModels;
using sasipca_API.Dtos;
using sasipca_API.Models;

namespace sasipca_API.Controllers
{
    /// <summary>
    /// Controller para consulta do histórico de movimentações de stock ou entregas
    /// </summary>
    [Route("api")]
    [ApiController]
    [Authorize]
    public class HistoryController : ControllerBase
    {
        private readonly SasipcaContext _dbContext;

        public HistoryController(SasipcaContext context)
        {
            _dbContext = context;
        }

        // ----------------------------------------------------
        // ENDPOINT 1: CONSULTA DO HISTÓRICO DE MOVIMENTOS GERAL (CABEÇALHOS)
        // ----------------------------------------------------
        /// <summary>
        /// Busca o histórico geral das movimentações de stock (cabeçalho, uma linha por movimento).
        /// </summary>
        /// <returns>Lista de movimentos resumidos.</returns>
        [HttpGet("movements")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<VMovHistory>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        public async Task<ActionResult<IEnumerable<VMovHistory>>> GetMovementHistory()
        {            
            var history = await _dbContext.VMovHistories
                .OrderByDescending(h => h.MovementDate)
                .ThenByDescending(h => h.MovementId)
                .ToListAsync();

            if (!history.Any())
            {
                return Ok(new List<VMovHistory>());
            }

            return Ok(history);
        }

        // ----------------------------------------------------
        // ENDPOINT 2: CONSULTA DE DETALHES DE UM MOVIMENTO
        // ----------------------------------------------------
        /// <summary>
        /// Busca todos os detalhes de um movimento específico, incluindo todos os itens de lote afetados.
        /// </summary>
        /// <param name="movementId">ID da movimentação.</param>
        /// <returns>Objeto contendo o cabeçalho do movimento e uma lista dos seus itens.</returns>
        [HttpGet("movements/{movementId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MovementDetailDTO))] // Usaremos um DTO de resposta aninhado
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        public async Task<ActionResult> GetMovementDetails(int movementId)
        {
           var details = await _dbContext.VMovHistoryDetails
                .Where(d => d.MovementId == movementId)
                .OrderBy(d => d.ProductBarcode) // Ordena os itens para consistência
                .ThenBy(d => d.ProductGroupId)
                .ToListAsync();

            if (!details.Any())
            {
                return NotFound(new Resposta($"Movimento não encontrado."));
            }

            // Mapeamento para um DTO de resposta estruturado (melhora a legibilidade da API)
            var structuredResponse = MapMovDetails(details);

            return Ok(structuredResponse);
        }



        // ----------------------------------------------------
        // ENDPOINT 3: CONSULTA DE ENTREGAS (COM FILTROS)
        // ----------------------------------------------------
        /// <summary>
        /// Retorna a lista de todas as entregas (cabeçalhos), com opções de filtragem por status, beneficiário e data.
        /// </summary>
        /// <param name="query">Parâmetros de filtro (StatusId, BeneficiaryId, DateFrom, DateTo).</param>
        /// <returns>Lista de cabeçalhos de entregas.</returns>
        [HttpGet("deliveries")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<VDelivery>))]
        public async Task<ActionResult<IEnumerable<VDelivery>>> GetDeliveries([FromQuery] DeliveryGetDTO query)
        {
            // Usamos a View pré-agregada que já contém todos os nomes e o status em formato string.
            var deliveriesQuery = _dbContext.VDeliveries.AsQueryable();

            // 1. Aplicação dos Filtros

            if (query.StatusId.HasValue)
            {
                deliveriesQuery = deliveriesQuery.Where(d => d.StatusId == (int)query.StatusId.Value); 
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



        // ----------------------------------------------------
        // ENDPOINT 4: CONSULTA DE DETALHES DE UMA ENTREGA
        // ----------------------------------------------------
        /// <summary>
        /// Busca todos os detalhes de uma entrega específica.
        /// </summary>
        /// <param name="deliveryID">ID da entrega.</param>
        /// <returns>Objeto contendo o cabeçalho da entrega e uma lista dos seus itens.</returns>
        [HttpGet("deliveries/{deliveryId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DeliveryDetailDTO))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        public async Task<ActionResult> GetDeliveryDetails(int deliveryId)
        {
            var details = await _dbContext.VDeliveriesDetails
                 .Where(d => d.DeliveryId == deliveryId)
                 .OrderBy(d => d.ProductBarcode)
                 .ThenBy(d => d.ProductGroupId)
                 .ToListAsync();

            if (!details.Any())
            {
                return NotFound(new Resposta($"Entrega com ID {deliveryId} não encontrada."));
            }

            var structuredResponse = MapDeliveryDetails(details);

            return Ok(structuredResponse);
        }







        // ----------------------------------------------------
        // FUNÇÃO PRIVADA DE ESTRUTURAÇÃO DA RESPOSTA
        // ----------------------------------------------------
        /// <summary>
        /// Mapeia a lista plana de detalhes da View para um objeto estruturado.
        /// </summary>
        private MovementDetailDTO MapMovDetails(List<VMovHistoryDetail> details)
        {
            var header = details.First();

            return new MovementDetailDTO
            {
                // Dados do Cabeçalho (Movimento)
                MovementId = header.MovementId,
                MovementDate = header.MovementDate,
                MovementTypeId = header.MovementTypeId,
                MovementNote = header.MovementNote,

                // Dados do Utilizador
                UserId = header.UserId,
                UserName = header.UserName,

                // Dados da Entrega (se Saída)
                DeliveryId = header.DeliveryId,

                // Itens de Movimentação (Lotes Afetados)
                Items = details.Select(d => new MovementItemDTO
                {
                    ItemQuantityAffected = d.ItemQuantityAffected,
                    ProductBarcode = d.ProductBarcode,
                    ProductName = d.ProductName,
                    ProductGroupId = d.ProductGroupId,
                    GroupExpiryDate = d.GroupExpiryDate
                }).ToList()
            };
        }

        // ----------------------------------------------------
        // FUNÇÃO PRIVADA DE ESTRUTURAÇÃO DA RESPOSTA
        // ----------------------------------------------------
        /// <summary>
        /// Mapeia a lista plana de detalhes da View para um objeto estruturado.
        /// </summary>
        private DeliveryDetailDTO MapDeliveryDetails(List<VDeliveriesDetail> details)
        {
            var header = details.First();

            return new DeliveryDetailDTO
            {
                // Cabeçalho da entrega
                DeliveryId = header.DeliveryId,
                ScheduledDate = header.ScheduledDate,
                StatusId = header.StatusId,
                Note = header.Note,

                // Dados do utilizador
                UserId = header.UserId,
                UserName = header.UserName,

                // Dados do beneficiário
                BeneficiaryId = header.BeneficiaryId,
                BeneficiaryName = header.BeneficiaryName,

                // Itens da entrega
                Items = details.Select(d => new DeliveryItemDTO
                {
                    Barcode = d.ProductBarcode,
                    groupId = d.ProductGroupId,
                    Quantity = d.ItemQuantity
                }).ToList()
            };
        }



    }
}