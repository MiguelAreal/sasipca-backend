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
    public class StockController : ControllerBase
    {
        private readonly SasipcaContext _dbContext;
        private readonly IDeliveryService _deliveryService;
        /// <summary>
        /// Inicialização do Stock Controller
        /// Lida com todas as movimentações de stock.
        /// </summary>
        public StockController(SasipcaContext context, IDeliveryService deliveryService)
        {
            _dbContext = context;
            _deliveryService = deliveryService;
        }

        // ----------------------------------------------------
        // ENDPOINT 1 : ENTRADA DE STOCK / CRIAÇÃO DE PRODUTO + PRIMEIRA ENTRADA
        // ----------------------------------------------------
        /// <summary>
        /// Regista uma Entrada de Stock. Pode criar um novo Produto e o seu stock inicial
        /// ou adicionar stock a lotes de um Produto já existente.
        /// </summary>
        [HttpPost("receipt")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        public async Task<ActionResult> StockReceipt([FromBody] StockReceiptDTO dto)
        {
            var userId = (int)HttpContext.Items["UserId"];
            var barcode = dto.Barcode;

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // O tipo de 'product' é inferido. Assumindo que o Barcode é a chave.
                var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Barcode == barcode);
                var isNewProduct = product == null;

                // 1. Validação de Dados de Criação
                if (isNewProduct)
                {
                    // Produto não existe. É obrigatório fornecer todos os dados mestre.
                    // Estou a assumir que 'dto' tem estas propriedades como 'string?', 'int?' etc.
                    if (string.IsNullOrEmpty(dto.Name) || dto.CategoryId == null || dto.UnitId == null)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new Resposta($"Produto com Barcode '{barcode}' não existe. É obrigatório fornecer o Nome, CategoryId e UnitId para a criação."));
                    }

                    // 2. Criar o Produto Mestre
                    product = new Product
                    {
                        Barcode = dto.Barcode,
                        Name = dto.Name!,
                        CategoryId = dto.CategoryId!.Value,
                        UnitId = dto.UnitId!.Value,
                        UnitSize = dto.UnitSize
                    };
                    _dbContext.Products.Add(product);
                }
                // ELSE: Produto existe. Os campos Name, CategoryId, etc. são ignorados.

                // 3. Criar o cabeçalho da Movimentação
                var newMovement = new Movement
                {
                    UserId = userId,
                    MovementTypeId = (int)Enums.MovementTypes.Entrada,
                    CreatedAt = DateTime.UtcNow,
                    Note = dto.Note ?? (isNewProduct ? "Criação de produto e primeira entrada de stock." : "Entrada de stock (receção).")
                };
                _dbContext.Movements.Add(newMovement);

                // Salvar para garantir o rastreio do produto/movimentação.
                await _dbContext.SaveChangesAsync();


                // 4. Processar Lotes
                foreach (var itemDto in dto.LotsToEnter)
                {
                    // Validação de lote: a quantidade tem que ser positiva para entrada
                    if (itemDto.Quantity <= 0)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new Resposta($"A quantidade para o lote '{itemDto.Lot}' deve ser positiva."));
                    }

                    var productLot = await _dbContext.ProductLots
                        .FirstOrDefaultAsync(pl => pl.Barcode == barcode && pl.Lot == itemDto.Lot);

                    if (productLot != null)
                    {
                        // Lote existe: Apenas adiciona a quantidade
                        productLot.Quantity += itemDto.Quantity;

                        if (productLot.ExpiryDate < itemDto.ExpiryDate)
                        {
                            productLot.ExpiryDate = itemDto.ExpiryDate;
                        }
                    }
                    else
                    {
                        // Lote não existe: Cria novo lote
                        productLot = new ProductLot
                        {
                            Barcode = barcode,
                            Lot = itemDto.Lot,
                            Quantity = itemDto.Quantity,
                            ExpiryDate = itemDto.ExpiryDate // Assumindo DateOnly?
                        };
                        _dbContext.ProductLots.Add(productLot);
                    }

                    // 5. Criar o Item da Movimentação (log)
                    newMovement.MovementItems.Add(new MovementItem
                    {
                        // Usa a instância productLot que foi criada ou carregada
                        ProductLot = productLot,
                        Quantity = itemDto.Quantity
                    });
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                var successMessage = isNewProduct
                    ? $"Produto '{dto.Name}' e lote(s) inicial(is) registados com sucesso."
                    : $"Entrada de stock para o produto '{barcode}' concluída com sucesso.";

                return StatusCode(StatusCodes.Status201Created, new Resposta(successMessage));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // _logger.LogError(ex, "Erro ao processar a entrada de stock unificada.");
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
        [HttpPatch("adjust")]
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
                // 1. Encontrar o lote específico
                var productLot = await _dbContext.ProductLots
                    .Include(pl => pl.BarcodeNavigation)
                    .FirstOrDefaultAsync(pl => pl.Barcode == dto.Barcode && pl.Lot == dto.Lot);

                if (productLot == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound(new Resposta($"Produto/Lote '{dto.Barcode}' - '{dto.Lot}' não encontrado."));
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
                        .Where(di => di.ProductLotId == productLot.Id && di.Delivery.StatusId == (int)Enums.DeliveryStatus.Agendada)
                        .SumAsync(di => di.Quantity);

                    // 2.2. Calcular Stock Disponível (Total - Reservado)
                    var availableStock = productLot.Quantity - reservedQuantity;

                    // 2.3. Validação: A quantidade a remover não pode exceder o stock disponível.
                    if (availableStock < quantityToAdjust)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new Resposta($"Ajuste de redução bloqueado. O stock disponível para ajuste é {availableStock}, mas está a tentar remover {quantityToAdjust}."));
                    }
                }

                // 3. Aplicar o Ajuste ao Lote (ProductLot)
                productLot.Quantity += adjustment;

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
                    ProductLot = productLot, // Lote atualizado
                    Quantity = adjustment
                });

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                var action = isReduction ? "removida" : "adicionada";
                return Ok(new Resposta($"Ajuste de stock concluído. Quantidade de {quantityToAdjust} {action} do produto '{productLot.BarcodeNavigation.Name}' (Lote: {dto.Lot})."));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Registrar a exceção 'ex'
                return StatusCode(StatusCodes.Status500InternalServerError, new Resposta("Ocorreu um erro interno ao processar o ajuste de stock."));
            }
        }

        // ----------------------------------------------------
        // ENDPOINT 3: SAÍDA ESPONTÂNEA (ENTREGUE IMEDIATAMENTE)
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
        // ENDPOINT 4: AGENDAR SAÍDA (PROGRAMADA)
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
        // ENDPOINT 5: ATUALIZAÇÃO E ALTERAÇÃO DE ESTADO DA ENTREGA
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
        // ENDPOINT 6: ELIMINA UMA ENTREGA AGENDADA
        // ----------------------------------------------------
        /// <summary>
        /// Elimina uma entrega agendada.
        /// Só é possível eliminar uma entrega se esta estiver como 'Agendada'.
        /// </summary>
        [HttpDelete("delivery/{deliveryId}")]
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


        // ----------------------------------------------------
        // ENDPOINT 7: CONSULTA DE ENTREGAS (COM FILTROS)
        // ----------------------------------------------------
        /// <summary>
        /// Retorna a lista de todas as entregas (cabeçalhos), com opções de filtragem por status, beneficiário e data.
        /// </summary>
        /// <param name="query">Parâmetros de filtro (StatusId, BeneficiaryId, DateFrom, DateTo).</param>
        /// <returns>Lista de cabeçalhos de entregas.</returns>
        [HttpGet("delivery")]
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