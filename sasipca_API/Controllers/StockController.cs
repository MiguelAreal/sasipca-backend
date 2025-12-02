using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sasipca_API.Attributes;
using sasipca_API.DBModels;
using sasipca_API.Dtos;
using sasipca_API.Enumerators;
using sasipca_API.Models;
using sasipca_API.Services;
using sasipca_API.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static sasipca_API.Enumerators.Enums;

namespace sasipca_API.Controllers
{
    /// <summary>
    /// Controller para gestão de stocks.
    /// </summary>
    [Route("api/stock")]
    [ApiController]
    [AuthorizeRole(UserRole.Admin)]
    public class StockController : ControllerBase
    {
        private readonly SasipcaContext _dbContext;
        private readonly IDeliveryService _deliveryService;
        private readonly IAuthService _authService;
        private readonly ITypesService _typesService;
        private readonly IJobSchedulerService _jobSchedulerService;

        /// <summary>
        /// Inicialização do Stock Controller
        /// Lida com todas as movimentações de stock.
        /// </summary>
        public StockController(SasipcaContext context, IDeliveryService deliveryService,
            IAuthService authService, ITypesService typesService, IJobSchedulerService jobSchedulerService)
        {
            _dbContext = context;
            _deliveryService = deliveryService;
            _authService = authService;
            _typesService = typesService;
            _jobSchedulerService = jobSchedulerService;
        }

        // ----------------------------------------------------
        // ENDPOINT 1 : ENTRADA DE STOCK / CRIAÇÃO DE PRODUTO + PRIMEIRA ENTRADA
        // ----------------------------------------------------
        /// <summary>
        /// Regista uma Entrada de Stock. Pode criar um novo Produto e o seu stock inicial
        /// ou adicionar stock a grupos de um Produto já existente.
        /// </summary>
        [HttpPost("receipts")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        public async Task<ActionResult> StockReceipt([FromBody] StockReceiptDTO dto)
        {
            int? userId = _authService.GetUserId();
            if (userId == null)
            {
                return Unauthorized(new Resposta("Utilizador não autenticado."));
            }

            var barcode = dto.Barcode;

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {               
                var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Barcode == barcode);
                var isNewProduct = product == null;

                // 1. Validação de Dados de Criação
                if (isNewProduct)
                {
                    // Produto não existe. É obrigatório fornecer todos os dados mestre.
                    if (string.IsNullOrEmpty(dto.Name) || dto.CategoryId == null || dto.UnitId == null)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new Resposta($"Produto não existe. É obrigatório fornecer o Nome, CategoryId e UnitId para a criação."));
                    }

                    // 2. Criar o Produto Mestre
                    product = new Product
                    {
                        Barcode = dto.Barcode,
                        Name = dto.Name!,
                        UnitSize = dto.UnitSize
                    };

                    // --- Associação à categoria ---
                    if (dto.CategoryId.HasValue)
                    {
                        if (!(await _typesService.VerifyCategory(dto.CategoryId.Value)))
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new Resposta($"Categoria '{dto.CategoryId.Value}' não encontrada."));
                        }

                        product.CategoryId = dto.CategoryId.Value;
                    }

                    // --- Associação a tipo de unidade ---
                    if (dto.UnitId.HasValue)
                    {
                        if (!(await _typesService.VerifyUnit(dto.UnitId.Value)))
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new Resposta($"Tipo de unidade '{dto.UnitId.Value}' não encontrada."));
                        }


                        product.UnitId = dto.UnitId.Value;
                    }


                    _dbContext.Products.Add(product);
                }

                // ELSE: Produto existe. Os campos Name, CategoryId, etc. são ignorados.
                // 3. Criar o cabeçalho da Movimentação
                var newMovement = new Movement
                {
                    UserId = userId.Value,
                    MovementTypeId = (int)Enums.MovementTypes.Entrada,
                    Note = dto.Note
                };

                // --- Associação opcional do movimento à campanha ---
                if (dto.campaignId.HasValue)
                {
                    var campaign = await _dbContext.Campaigns
                        .FirstOrDefaultAsync(c => c.Id == dto.campaignId.Value);

                    if (campaign == null)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new Resposta($"Campanha '{dto.campaignId.Value}' não encontrada."));
                    }

                    newMovement.CampaignId = campaign.Id;
                }

                _dbContext.Movements.Add(newMovement);
                await _dbContext.SaveChangesAsync();


                // 4. Processar Grupos
                foreach (var itemDto in dto.Groups)
                {
                    // Validação de grupo: a quantidade tem que ser positiva para entrada
                    if (itemDto.Quantity <= 0)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new Resposta($"A quantidade para o grupo deve ser positiva."));
                    }

                    var productGroup = await _dbContext.ProductGroups
                        .FirstOrDefaultAsync(pl => pl.Barcode == barcode && pl.ExpiryDate == itemDto.ExpiryDate);

                    if (productGroup != null)
                    {
                        // Grupo existe: Apenas adiciona a quantidade
                        productGroup.Quantity += itemDto.Quantity;
                    }
                    else
                    {
                        // Grupo não existe: Cria novo grupo
                        productGroup = new ProductGroup
                        {
                            Barcode = barcode,
                            Quantity = itemDto.Quantity,
                            ExpiryDate = itemDto.ExpiryDate
                        };
                        _dbContext.ProductGroups.Add(productGroup);
                    }

                    // 5. Criar o Item da Movimentação
                    newMovement.MovementItems.Add(new MovementItem
                    {
                        // Usa a instância productGroup que foi criada ou carregada
                        ProductGroup = productGroup,
                        Quantity = itemDto.Quantity
                    });
                }

                await _dbContext.SaveChangesAsync();

                // Só agendamos se o produto tiver a configuração de dias de aviso (ExpNotif) definida.
                if (product.ExpNotif.HasValue)
                {
                    // Iteramos sobre os itens do movimento que acabámos de criar.
                    // Como já fizemos SaveChanges, o 'item.ProductGroup.Id' já existe.
                    foreach (var item in newMovement.MovementItems)
                    {
                        // O ProductGroup está carregado em memória via Entity Framework
                        var group = item.ProductGroup;

                        _jobSchedulerService.ScheduleExpiryCheck(
                            groupId: group.Id,
                            productName: product.Name,
                            expiryDate: group.ExpiryDate,
                            daysBefore: product.ExpNotif.Value
                        );
                    }
                }

                await transaction.CommitAsync();

                var successMessage = isNewProduct
                    ? $"Produto '{dto.Name}' e grupo(s) inicial(is) registados com sucesso."
                    : $"Entrada de stock para o produto '{barcode}' concluída com sucesso.";

                return StatusCode(StatusCodes.Status201Created, new Resposta(successMessage));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(StatusCodes.Status500InternalServerError, new Resposta("Ocorreu um erro interno ao processar a entrada de stock."));
            }
        }


        // ----------------------------------------------------
        // ENDPOINT 2 : AJUSTE / CORREÇÃO DE STOCK
        // ----------------------------------------------------
        /// <summary>
        /// Ajusta (adiciona ou remove) a quantidade de stock de um lote existente,
        /// registando uma Movimentação de Ajuste.
        /// A remoção está limitada ao stock disponível (Total - Reservado).
        /// </summary>
        [HttpPatch("adjusts")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        public async Task<ActionResult> AdjustStock([FromBody] StockAdjustmentDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Obter UserId
            var userId = (int)HttpContext.Items["UserId"];

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // 1. Encontrar o grupo específico
                var productGroup = await _dbContext.ProductGroups
                    .Include(pl => pl.BarcodeNavigation)
                    .FirstOrDefaultAsync(pl => pl.Barcode == dto.Barcode && pl.Id == dto.GroupId);

                if (productGroup == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound(new Resposta($"Produto/Grupo '{dto.Barcode}' - '{dto.GroupId}' não encontrado."));
                }

                var adjustment = dto.QuantityAdjustment;
                var isReduction = adjustment < 0;
                var quantityToAdjust = Math.Abs(adjustment); // Valor absoluto

                // 2. Validação para Redução (Saída de Stock)
                if (isReduction)
                {
                    // 2.1. Calcular Stock Reservado
                    // Stock reservado são todas as DeliveryItems em Deliveries com StatusId = Agendada (1)
                    var reservedQuantity = await _dbContext.DeliveryItems
                        .Where(di => di.ProductGroupId == productGroup.Id && di.Delivery.StatusId == (int)Enums.DeliveryStatus.Agendada)
                        .SumAsync(di => di.Quantity);

                    // 2.2. Calcular Stock Disponível (Total - Reservado)
                    var availableStock = productGroup.Quantity - reservedQuantity;

                    // 2.3. Validação: A quantidade a remover não pode exceder o stock disponível.
                    if (availableStock < quantityToAdjust)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new Resposta($"Ajuste de redução bloqueado. O stock disponível para ajuste é {availableStock}, mas está a tentar remover {quantityToAdjust}."));
                    }
                }

                // 3. Aplicar o Ajuste ao Lote (ProductLot)
                productGroup.Quantity += adjustment;

                // 4. Criar a Movimentação (Movement)
                var newMovement = new Movement
                {
                    UserId = userId,
                    MovementTypeId = (int)Enums.MovementTypes.AjusteInventario,
                    Note = dto.Note
                };
                _dbContext.Movements.Add(newMovement);
                await _dbContext.SaveChangesAsync(); // Commit para obter Movement.Id (necessário para MovementItem)

                // 5. Criar o Item da Movimentação (MovementItem)
                newMovement.MovementItems.Add(new MovementItem
                {
                    ProductGroup = productGroup, // Lote atualizado
                    Quantity = adjustment
                });

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                var action = isReduction ? "removida" : "adicionada";
                return Ok(new Resposta($"Ajuste de stock concluído. Quantidade de {quantityToAdjust} {action} do produto '{productGroup.BarcodeNavigation.Name}'."));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Registrar a exceção 'ex'
                return StatusCode(StatusCodes.Status500InternalServerError, new Resposta("Ocorreu um erro interno ao processar o ajuste de stock."));
            }
        }

       
        // ----------------------------------------------------
        // ENDPOINT 3: AGENDAR SAÍDA / EXECUTAR SAÍDA
        // ----------------------------------------------------
        /// <summary>
        /// Agenda uma Entrega futura a um Beneficiário -> Não deduz stock, apenas cria o registo e marca como agendada.
        /// Executa saída imediata -> Deduz stock, cria registo e marca como concluída.
        /// </summary>
        /// <param name="dto">DTO com payload de dados.</param>
        /// <param name="instant">TRUE se for entrega imediata, FALSE se for para agendar.</param>
        [HttpPost("deliveries")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        public async Task<ActionResult> CreateDelivery([FromQuery] bool instant, [FromBody] DeliveryPostDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // 1. Obter UserId do Context
            int? userId = _authService.GetUserId();
            if (userId == null)
            {
                return Unauthorized(new Resposta("Utilizador não autenticado."));
            }

            // Validação da data apenas para agendadas
            if (!instant && dto.ScheduledDate < DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new Resposta("A data agendada deve ser futura."));
            }

            // Se for uma entrega imediata, define-se a data de entrega como HOJE.
            if (instant)
            {
                dto.ScheduledDate = DateOnly.FromDateTime(DateTime.Today);
            }

            // 3. Chamar o Serviço
            var (success, result) = await _deliveryService.CreateDelivery(
               dto,
               userId.Value,
               instant ? Enums.DeliveryStatus.Entregue : Enums.DeliveryStatus.Agendada,
               instant // deduz stock?
            );

            return success
            ? StatusCode(StatusCodes.Status201Created, result)
            : BadRequest(result);
        }

        // ----------------------------------------------------
        // ENDPOINT 4: ATUALIZAÇÃO E ALTERAÇÃO DE ESTADO DA ENTREGA
        // ----------------------------------------------------
        /// <summary>
        /// Atualiza os dados de uma entrega agendada (data, itens ou status).
        /// Fluxo de estado: Agendada -> [Agendada, Entregue, Cancelada]. Outros estados são finais.
        /// </summary>
        [HttpPut("deliveries/{deliveryId}")]
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
        // ENDPOINT 5: ELIMINA UMA ENTREGA AGENDADA
        // ----------------------------------------------------
        /// <summary>
        /// Elimina uma entrega agendada.
        /// Só é possível eliminar uma entrega se esta estiver como 'Agendada'.
        /// </summary>
        [HttpDelete("deliveries/{deliveryId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        public async Task<ActionResult> DeleteDelivery(int deliveryId)
        {

            // 1. Chamar o Serviço para executar a lógica de eliminar entrega
            var (success, result) = await _deliveryService.DeleteDelivery(
                deliveryId
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

       
    }

}