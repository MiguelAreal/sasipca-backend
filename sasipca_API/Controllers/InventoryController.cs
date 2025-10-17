using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sasipca_API.DBModels;
using sasipca_API.Dtos;
using sasipca_API.Enumerators;
using sasipca_API.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // Se estiver a usar logging

[Route("api/inventory")]
[ApiController]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly SasipcaContext _dbContext;
    // private readonly ILogger<InventoryController> _logger; // Exemplo de logger

    public InventoryController(SasipcaContext context /*, ILogger<InventoryController> logger */)
    {
        _dbContext = context;
        // _logger = logger;
    }

    // ----------------------------------------------------
    // ENDPOINT 1: CRIAÇÃO DE PRODUTO E PRIMEIRA ENTRADA (NOVO)
    // ----------------------------------------------------
    /// <summary>
    /// Regista um novo Produto e a sua primeira entrada de Stock, criando o lote inicial.
    /// (Deve ser usado quando o GET/consulta inicial devolve 404).
    /// </summary>
    [HttpPost("product-receipt")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
    public async Task<ActionResult> RegisterProductAndInitialStock([FromBody] ProductCreationReceiptDTO dto)
    {
        var userId = (int)HttpContext.Items["UserId"];

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // 1. Validação de Existência (Não deve existir)
        if (await _dbContext.Products.AnyAsync(p => p.Barcode == dto.Barcode))
        {
            return BadRequest(new Resposta($"O Produto com o Barcode '{dto.Barcode}' já existe. Use o endpoint 'receipt' para adicionar stock."));
        }

        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // 2. Criar o Produto Mestre
            var newProduct = new Product
            {
                Barcode = dto.Barcode,
                Name = dto.Name,
                CategoryId = dto.CategoryId,
                UnitId = dto.UnitId,
                UnitSize = dto.UnitSize
            };
            _dbContext.Products.Add(newProduct);

            // 3. Criar o Lote Inicial (ProductLot)
            var newLot = new ProductLot
            {
                Barcode = dto.Barcode,
                Lot = dto.InitialLot.Lot,
                Quantity = dto.InitialLot.Quantity,
                ExpiryDate = dto.InitialLot.ExpiryDate
            };
            _dbContext.ProductLots.Add(newLot);

            // 4. Criar o cabeçalho da Movimentação (Entrada/1)
            var newMovement = new Movement
            {
                UserId = userId,
                MovementTypeId = (int)Enums.MovementTypes.Entrada,
                CreatedAt = DateTime.UtcNow,
                Note = dto.Note ?? "Criação de produto e primeira entrada de stock."
            };
            _dbContext.Movements.Add(newMovement);

            // 5. Criar o Item da Movimentação (log)
            newMovement.MovementItems.Add(new MovementItem
            {
                ProductLot = newLot,
                Quantity = newLot.Quantity
            });

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return StatusCode(StatusCodes.Status201Created, new Resposta($"Produto '{dto.Name}' e lote inicial registados com sucesso."));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            // _logger.LogError(ex, "Erro ao registar produto e primeira entrada de stock.");
            return StatusCode(StatusCodes.Status500InternalServerError, new Resposta("Ocorreu um erro interno ao criar o produto e o stock inicial."));
        }
    }


    // ----------------------------------------------------
    // ENDPOINT 2: ENTRADA DE STOCK (RECEIPT) - PARA PRODUTOS JÁ EXISTENTES
    // ----------------------------------------------------
    /// <summary>
    /// Regista uma Entrada de Stock, criando lotes ou adicionando stock a lotes existentes.
    /// (Requer que o produto já exista.)
    /// </summary>
    [HttpPost("receipt")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))] // Adicionado 404
    public async Task<ActionResult> RegisterReceipt([FromBody] StockReceiptDTO dto)
    {
        var userId = (int)HttpContext.Items["UserId"];

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!dto.LotsToEnter.Any())
        {
            return BadRequest(new Resposta("A movimentação de entrada deve conter pelo menos um lote."));
        }

        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var barcode = dto.Barcode;

            // 1. Validação de Existência (ESTRITAMENTE OBRIGATÓRIA)
            var productExists = await _dbContext.Products.AnyAsync(p => p.Barcode == barcode);

            if (!productExists)
            {
                transaction.Rollback();
                // Retorna 404, forçando o cliente a usar o endpoint "product-receipt"
                return NotFound(new Resposta($"Produto com Barcode '{barcode}' não encontrado. Use 'product-receipt' para registar novos produtos."));
            }

            // 2. Criar o cabeçalho da Movimentação
            var newMovement = new Movement
            {
                UserId = userId,
                MovementTypeId = (int)Enums.MovementTypes.Entrada,
                Note = dto.Note
            };
            _dbContext.Movements.Add(newMovement);
            await _dbContext.SaveChangesAsync();

            // 3. Processar Lotes
            foreach (var itemDto in dto.LotsToEnter)
            {
                var productLot = await _dbContext.ProductLots
                    .FirstOrDefaultAsync(pl => pl.Barcode == barcode && pl.Lot == itemDto.Lot);

                if (productLot != null)
                {
                    // Lote existe: Apenas adiciona a quantidade
                    productLot.Quantity += itemDto.Quantity;
                }
                else
                {
                    // Lote não existe: Cria novo lote
                    productLot = new ProductLot
                    {
                        Barcode = barcode,
                        Lot = itemDto.Lot,
                        Quantity = itemDto.Quantity,
                        ExpiryDate = itemDto.ExpiryDate
                    };
                    _dbContext.ProductLots.Add(productLot);
                }

                // 4. Criar o Itens da Movimentação (log)
                newMovement.MovementItems.Add(new MovementItem
                {
                    ProductLot = productLot,
                    Quantity = itemDto.Quantity
                });
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return StatusCode(StatusCodes.Status201Created, new Resposta("Entrada de stock e registo de lotes concluídos com sucesso."));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            // _logger.LogError(ex, "Erro ao registar entrada de stock.");
            return StatusCode(StatusCodes.Status500InternalServerError, new Resposta("Ocorreu um erro interno ao processar a entrada de stock."));
        }
    }
}