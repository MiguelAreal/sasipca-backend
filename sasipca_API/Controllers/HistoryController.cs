using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sasipca_API.Attributes;
using sasipca_API.DBModels;
using sasipca_API.Dtos;
using sasipca_API.Enumerators; // Importante para o Enum UserRole
using sasipca_API.Models;
using System.Security.Claims;
using static sasipca_API.Enumerators.Enums; // Necessário para ler claims

namespace sasipca_API.Controllers
{
    /// <summary>
    /// Controller para consulta do histórico de movimentações de stock ou entregas
    /// </summary>
    [Route("api")]
    [ApiController]
    // Permite que tanto Admins como Beneficiários acedam ao controller
    [AuthorizeRole(UserRole.Admin, UserRole.Beneficiary)]
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
        [AuthorizeRole(UserRole.Admin)] // Apenas Admins devem ver movimentos de stock internos
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
        [AuthorizeRole(UserRole.Admin)] // Apenas Admins
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MovementDetailDTO))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        public async Task<ActionResult> GetMovementDetails(int movementId)
        {
            var details = await _dbContext.VMovHistoryDetails
                 .Where(d => d.MovementId == movementId)
                 .OrderBy(d => d.ProductBarcode)
                 .ThenBy(d => d.ProductGroupId)
                 .ToListAsync();

            if (!details.Any())
            {
                return NotFound(new Resposta($"Movimento não encontrado."));
            }

            var structuredResponse = MapMovDetails(details);

            return Ok(structuredResponse);
        }

        // ----------------------------------------------------
        // ENDPOINT 3: CONSULTA DE ENTREGAS (COM FILTROS)
        // ----------------------------------------------------
        /// <summary>
        /// Retorna a lista de todas as entregas (cabeçalhos).
        /// Se Admin: Pode ver tudo e filtrar por qualquer beneficiário.
        /// Se Beneficiário: Vê apenas as suas próprias entregas.
        /// </summary>
        /// <param name="query">Parâmetros de filtro (StatusId, BeneficiaryId, DateFrom, DateTo).</param>
        /// <returns>Lista de cabeçalhos de entregas.</returns>
        [HttpGet("deliveries")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<VDelivery>))]
        public async Task<ActionResult<IEnumerable<VDelivery>>> GetDeliveries([FromQuery] DeliveryGetDTO query)
        {
            // 1. Identificar o Utilizador e o seu Role
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var roleStr = User.FindFirstValue(ClaimTypes.Role);

            if (userIdStr == null || roleStr == null || !int.TryParse(userIdStr, out int userId) || !Enum.TryParse(roleStr, out UserRole userRole))
            {
                return Unauthorized(new Resposta("Utilizador não autenticado ou token inválido."));
            }

            var deliveriesQuery = _dbContext.VDeliveries.AsQueryable();

            // 2. Aplicar Restrições de Segurança baseadas no Role
            if (userRole == UserRole.Beneficiary)
            {
                // Beneficiário só vê as suas entregas
                deliveriesQuery = deliveriesQuery.Where(d => d.BeneficiaryId == userId);
            }
            else if (userRole == UserRole.Admin)
            {
                // Admin pode filtrar por beneficiário se quiser
                if (query.BeneficiaryId.HasValue)
                {
                    deliveriesQuery = deliveriesQuery.Where(d => d.BeneficiaryId == query.BeneficiaryId.Value);
                }
            }

            // 3. Aplicação dos Filtros Comuns (Status e Datas)
            if (query.StatusId.HasValue)
            {
                deliveriesQuery = deliveriesQuery.Where(d => d.StatusId == (int)query.StatusId.Value);
            }

            if (query.DateFrom.HasValue)
            {
                deliveriesQuery = deliveriesQuery.Where(d => d.ScheduledDate >= DateOnly.FromDateTime(query.DateFrom.Value));
            }

            if (query.DateTo.HasValue)
            {
                deliveriesQuery = deliveriesQuery.Where(d => d.ScheduledDate <= DateOnly.FromDateTime(query.DateTo.Value));
            }

            // 4. Execução da Query
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
        /// Valida se o beneficiário tem permissão para ver esta entrega.
        /// </summary>
        /// <param name="deliveryId">ID da entrega.</param>
        /// <returns>Objeto contendo o cabeçalho da entrega e uma lista dos seus itens.</returns>
        [HttpGet("deliveries/{deliveryId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DeliveryDetailDTO))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(Resposta))] // Novo: Proibido
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        public async Task<ActionResult> GetDeliveryDetails(int deliveryId)
        {
            // 1. Identificar o Utilizador
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var roleStr = User.FindFirstValue(ClaimTypes.Role);

            if (userIdStr == null || roleStr == null || !int.TryParse(userIdStr, out int userId) || !Enum.TryParse(roleStr, out UserRole userRole))
            {
                return Unauthorized(new Resposta("Utilizador não autenticado."));
            }

            // 2. Buscar detalhes
            var details = await _dbContext.VDeliveriesDetails
                 .Where(d => d.DeliveryId == deliveryId)
                 .OrderBy(d => d.ProductBarcode)
                 .ThenBy(d => d.ProductGroupId)
                 .ToListAsync();

            if (!details.Any())
            {
                return NotFound(new Resposta($"Entrega com ID {deliveryId} não encontrada."));
            }

            // 3. Validação de Segurança: Verificar se a entrega pertence ao beneficiário
            // O campo BeneficiaryId deve existir na view VDeliveriesDetails
            var deliveryOwnerId = details.First().BeneficiaryId;

            if (userRole == UserRole.Beneficiary && deliveryOwnerId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new Resposta("Não tem permissão para visualizar esta entrega."));
            }

            var structuredResponse = MapDeliveryDetails(details);

            return Ok(structuredResponse);
        }

        // ----------------------------------------------------
        // ENDPOINT 5: HISTÓRICO DE UM PRODUTO ESPECÍFICO
        // ----------------------------------------------------
        /// <summary>
        /// Busca o histórico de movimentos onde um produto específico esteve envolvido.
        /// Retorna os cabeçalhos dos movimentos (Data, Tipo, Utilizador, etc).
        /// </summary>
        /// <param name="barcode">Código de barras do produto.</param>
        [HttpGet("products/{barcode}/history")]
        [AuthorizeRole(UserRole.Admin)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<VMovHistory>))]
        public async Task<ActionResult<IEnumerable<VMovHistory>>> GetProductHistory(string barcode)
        {
            // 1. Encontrar IDs dos movimentos onde o produto participa
            // Usamos a View de Detalhes para filtrar pelo Barcode
            var movementIds = await _dbContext.VMovHistoryDetails
                .Where(d => d.ProductBarcode == barcode)
                .Select(d => d.MovementId)
                .Distinct()
                .ToListAsync();

            if (!movementIds.Any())
            {
                return Ok(new List<VMovHistory>());
            }

            // 2. Buscar os cabeçalhos desses movimentos
            // Usamos a View de Cabeçalhos (VMovHistory)
            var history = await _dbContext.VMovHistories
                .Where(h => movementIds.Contains(h.MovementId))
                .OrderByDescending(h => h.MovementDate)
                .ThenByDescending(h => h.MovementId)
                .ToListAsync();

            return Ok(history);
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
                Items = details.Select(d => new DeliveryItemGetDTO
                {
                    Name = d.ProductName,
                    Barcode = d.ProductBarcode,
                    GroupId = d.ProductGroupId,
                    ExpiryDate = d.GroupExpiryDate,
                    Quantity = d.ItemQuantity
                }).ToList()
            };
        }



    }
}