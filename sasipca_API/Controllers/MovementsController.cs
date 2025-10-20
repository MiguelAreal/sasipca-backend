using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sasipca_API.DBModels;
using sasipca_API.Dtos;
using sasipca_API.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sasipca_API.Controllers
{
    /// <summary>
    /// Controller para consulta do histórico de movimentações de stock.
    /// </summary>
    [Route("api/movements")]
    [ApiController]
    [Authorize]
    public class MovementsController : ControllerBase
    {
        private readonly SasipcaContext _dbContext;

        public MovementsController(SasipcaContext context)
        {
            _dbContext = context;
        }

        // ----------------------------------------------------
        // ENDPOINT 1: CONSULTA DO HISTÓRICO GERAL (CABEÇALHOS)
        // ----------------------------------------------------
        /// <summary>
        /// Busca o histórico geral das movimentações de stock (cabeçalho, uma linha por movimento).
        /// </summary>
        /// <returns>Lista de movimentos resumidos.</returns>
        [HttpGet("history")]
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
        [HttpGet("details/{movementId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MovementDetailDTO))] // Usaremos um DTO de resposta aninhado
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        public async Task<ActionResult> GetMovementDetails(int movementId)
        {
           var details = await _dbContext.VMovHistoryDetails
                .Where(d => d.MovementId == movementId)
                .OrderBy(d => d.ProductBarcode) // Ordena os itens para consistência
                .ThenBy(d => d.ProductLotNumber)
                .ToListAsync();

            if (!details.Any())
            {
                return NotFound(new Resposta($"Movimento com ID {movementId} não encontrado."));
            }

            // Mapeamento para um DTO de resposta estruturado (melhora a legibilidade da API)
            var structuredResponse = MapDetailsToStructuredDTO(details);

            return Ok(structuredResponse);
        }



        // ----------------------------------------------------
        // FUNÇÃO PRIVADA DE ESTRUTURAÇÃO DA RESPOSTA
        // ----------------------------------------------------
        /// <summary>
        /// Mapeia a lista plana de detalhes da View para um objeto estruturado.
        /// </summary>
        private MovementDetailDTO MapDetailsToStructuredDTO(List<VMovHistoryDetail> details)
        {
            var header = details.First();

            return new MovementDetailDTO
            {
                // Dados do Cabeçalho (Movimento)
                MovementId = header.MovementId,
                MovementDate = header.MovementDate,
                MovementType = header.MovementType,
                MovementNote = header.MovementNote,

                // Dados do Utilizador
                UserId = header.UserId,
                UserName = header.UserName,

                // Dados da Entrega (se Saída)
                DeliveryId = header.DeliveryId,
                DeliveryScheduledDate = header.DeliveryScheduledDate,
                BeneficiaryId = header.BeneficiaryId,
                BeneficiaryName = header.BeneficiaryName,

                // Itens de Movimentação (Lotes Afetados)
                Items = details.Select(d => new MovementItemDTO
                {
                    QuantityAffected = d.ItemQuantityAffected,
                    ProductBarcode = d.ProductBarcode,
                    ProductName = d.ProductName,
                    ProductLotNumber = d.ProductLotNumber,
                    LotExpiryDate = d.LotExpiryDate
                }).ToList()
            };
        }
    }
}