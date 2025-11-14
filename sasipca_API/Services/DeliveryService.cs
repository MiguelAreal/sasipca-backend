using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sasipca_API.DBModels;
using sasipca_API.Dtos;
using sasipca_API.Enumerators;
using sasipca_API.Models;
using sasipca_API.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static sasipca_API.Enumerators.Enums;

namespace sasipca_API.Services
{
    /// <summary>
    /// Serviço responsável pela gestão de Entregas (Delivery) e saída de stock.
    /// </summary>
    public class DeliveryService : IDeliveryService
    {
        private readonly SasipcaContext _dbContext;

        public DeliveryService(SasipcaContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ====================================================================
        // FUNÇÃO PRIVADA ABSTRAÍDA: PROCESSA ITENS, VALIDA STOCK E EXPIRAÇÃO
        // ====================================================================
        /// <summary>
        /// Processa a lista de itens de entrega: valida, cria DeliveryItems, e opcionalmente
        /// deduz o stock e cria os MovementItems.
        /// </summary>
        /// <param name="delivery">A instância da Delivery atual.</param>
        /// <param name="itemDtos">Os itens a serem processados.</param>
        /// <param name="scheduledDate">A data agendada para validação de expiração.</param>
        /// <param name="newMovement">A instância do Movement para log (ou null se não houver dedução).</param>
        /// <param name="lotsToUpdate">Lista para acumular lotes a serem atualizados (dedução).</param>
        /// <returns>Tuplo com sucesso e Resposta (se falhar).</returns>
        private async Task<(bool success, Resposta? response)> ProcessDeliveryItems(
            Delivery delivery,
            List<DeliveryItemDTO> itemDtos,
            DateOnly scheduledDate,
            Movement? newMovement,
            List<ProductLot> lotsToUpdate)
        {
            var isDeductingStock = newMovement != null;
            var isScheduling = delivery.StatusId == (int)Enums.DeliveryStatus.Agendada;

            foreach (var itemDto in itemDtos)
            {
                // Encontrar o lote pelo Barcode e Lot
                var productLot = await _dbContext.ProductLots
                    .Include(pl => pl.BarcodeNavigation)
                    .FirstOrDefaultAsync(pl => pl.Barcode == itemDto.Barcode && pl.Lot == itemDto.Lot);

                if (productLot == null)
                {
                    return (false, new Resposta($"Lote '{itemDto.Lot}' do produto '{itemDto.Barcode}' não encontrado."));
                }

                // 1. VALIDAÇÃO DE EXPIRAÇÃO (Apenas para agendamento)
                if (isScheduling)
                {
                    // Bloquear se a data de validade for ANTES ou NO DIA da entrega agendada.
                    if (productLot.ExpiryDate <= scheduledDate)
                    {
                        return (false, new Resposta($"Agendamento bloqueado. O produto '{productLot.BarcodeNavigation.Name}' (Lote: {itemDto.Lot}, Válido até: {productLot.ExpiryDate:yyyy-MM-dd}) expira antes ou no dia ({scheduledDate:yyyy-MM-dd}) da entrega agendada."));
                    }
                }

                // 2. VALIDAÇÃO DE STOCK DISPONÍVEL (Sempre necessária para reserva ou saída)

                // 2.1. Obter a quantidade TOTAL reservada (excluindo a entrega atual, se for atualização)
                var reservedQuantity = await _dbContext.DeliveryItems
                    .Where(di => di.ProductLotId == productLot.Id
                            && di.Delivery.StatusId == (int)Enums.DeliveryStatus.Agendada
                            && di.DeliveryId != delivery.Id) // Excluir reservas desta mesma entrega se for UPDATE
                    .SumAsync(di => di.Quantity);

                var availableStock = productLot.Quantity - reservedQuantity;

                // 2.2. Validação: Stock Disponível vs. Quantidade Solicitada
                if (availableStock < itemDto.Quantity)
                {
                    return (false, new Resposta($"Stock insuficiente para reserva/saída. Produto '{productLot.BarcodeNavigation.Name}' (Lote: {itemDto.Lot}). Stock Disponível: {availableStock}, Quantidade solicitada: {itemDto.Quantity}."));
                }

                // 3. CRIAÇÃO DO DELIVERY ITEM
                delivery.DeliveryItems.Add(new DeliveryItem
                {
                    DeliveryId = delivery.Id,
                    ProductLotId = productLot.Id,
                    Quantity = (int)itemDto.Quantity
                });

                // 4. DEDUÇÃO DE STOCK e LOG (Apenas se for Saída Imediata)
                if (isDeductingStock)
                {
                    productLot.Quantity -= (int)itemDto.Quantity;
                    lotsToUpdate.Add(productLot); // Marcar para atualização em lote

                    if (newMovement != null)
                    {
                        newMovement.MovementItems.Add(new MovementItem
                        {
                            ProductLot = productLot,
                            Quantity = -(int)itemDto.Quantity // Negativo para Saída
                        });
                    }
                }
            }

            return (true, null);
        }

        // ====================================================================
        // MÉTODO PÚBLICO: CREATE DELIVERY
        // ====================================================================
        public async Task<(bool success, Resposta? response)> CreateDelivery(
            DeliveryPostDTO dto,
            int userId,
            Enums.DeliveryStatus initialStatus,
            bool deductStock)
        {
            // 1. Validação de Beneficiário
            var beneficiaryExists = await _dbContext.Beneficiaries
                .AnyAsync(b => b.Id == dto.BeneficiaryId);

            if (!beneficiaryExists)
            {
                return (false, new Resposta($"Beneficiário com ID '{dto.BeneficiaryId}' não encontrado."));
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // 2. Criar cabeçalho da Entrega (Delivery)
                var newDelivery = new Delivery
                {
                    BeneficiaryId = dto.BeneficiaryId,
                    UserId = userId,
                    StatusId = (int)initialStatus,
                    ScheduledDate = dto.ScheduledDate,
                    Note = dto.Note,
                };
                _dbContext.Deliveries.Add(newDelivery);
                await _dbContext.SaveChangesAsync(); // Para obter o Delivery.Id

                // 3. Criar o cabeçalho da Movimentação
                Movement? newMovement = null;
                var lotsToUpdate = new List<ProductLot>();

                // Se for para deduzir stock (é uma saída espontânea)
                if (deductStock)
                {
                    newMovement = new Movement
                    {
                        UserId = userId,
                        MovementTypeId = (int)MovementTypes.Saida,
                        Delivery = newDelivery,
                        Note = dto.Note
                    };
                    _dbContext.Movements.Add(newMovement);
                }

                // 4. Processar Itens da Entrega (usa função abstraída)
                var (success, result) = await ProcessDeliveryItems(
                    newDelivery,
                    dto.ItemsToDeliver,
                    dto.ScheduledDate,
                    newMovement,
                    lotsToUpdate);

                if (!success)
                {
                    await transaction.RollbackAsync();
                    return (false, result);
                }

                // 5. Finalizar transação
                if (deductStock && newMovement != null)
                {
                    _dbContext.ProductLots.UpdateRange(lotsToUpdate);
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                var message = initialStatus == Enums.DeliveryStatus.Entregue
                    ? $"Entrega imediata ID {newDelivery.Id} registada e stock deduzido."
                    : $"Entrega agendada ID {newDelivery.Id} programada com sucesso.";

                return (true, new Resposta(message));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, new Resposta("Ocorreu um erro interno ao processar a entrega de stock."));
            }
        }

        // ====================================================================
        // MÉTODO PÚBLICO: Atualizar entrega existente (apenas agendadas)
        // ====================================================================
        public async Task<(bool success, Resposta? response)> UpdateDelivery(
            int deliveryId,
            DeliveryUpdateDTO dto,
            int userId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var delivery = await _dbContext.Deliveries
                    .Include(d => d.DeliveryItems)
                    .FirstOrDefaultAsync(d => d.Id == deliveryId);

                if (delivery == null) return (false, new Resposta($"Entrega com ID {deliveryId} não encontrado."));


                // 1. VALIDAÇÃO DE ESTADO (Estados Finais não podem mudar)
                if (delivery.StatusId == (int)Enums.DeliveryStatus.Entregue || delivery.StatusId == (int)Enums.DeliveryStatus.Cancelada)
                {
                    await transaction.RollbackAsync();
                    return (false, new Resposta($"A entrega está em estado final ('{((Enums.DeliveryStatus)delivery.StatusId)}') e não pode ser alterada."));
                }

                var oldStatus = delivery.StatusId;

                var newStatus = dto.NewStatusId;

                // Variáveis para atualização
                var newScheduledDate = dto.ScheduledDate ?? delivery.ScheduledDate;
                var lotsToUpdate = new List<ProductLot>();
                Movement? newMovement = null;

                // 3. PROCESSAMENTO DA TRANSIÇÃO DE ESTADO
                if (newStatus == (int)Enums.DeliveryStatus.Cancelada)
                {
                    // 3.1. Cancelar: Apenas atualizar o status e nota.
                    delivery.StatusId = (int)Enums.DeliveryStatus.Cancelada;
                    delivery.Note = dto.Note ?? delivery.Note;
                }
                else if (newStatus == (int)Enums.DeliveryStatus.Entregue)
                {
                    // 3.2. ENTREGUE: Requer validação de STOCK/VALIDADE e DEDUÇÃO imediata.

                    // a) Criar cabeçalho de Movimentação (Saída)
                    newMovement = new Movement
                    {
                        UserId = userId,
                        MovementTypeId = (int)MovementTypes.Saida,
                        Delivery = delivery,
                        Note = dto.Note
                    };
                    _dbContext.Movements.Add(newMovement);

                    // b) Processar Itens com dedução de stock
                    var (success, result) = await ProcessDeliveryItems(
                        delivery,
                        dto.ItemsToDeliver,
                        newScheduledDate,
                        newMovement,
                        lotsToUpdate);

                    if (!success)
                    {
                        await transaction.RollbackAsync();
                        return (false, result);
                    }

                    // c) Atualizar status e nota
                    delivery.StatusId = (int)Enums.DeliveryStatus.Entregue;
                    delivery.Note = dto.Note ?? delivery.Note;
                }
                else // Status permanece Agendada (newStatus == DeliveryStatus.Agendada)
                {
                    // 3.3. RE-AGENDAR/ATUALIZAR ITENS: Requer validação (sem dedução de stock)

                    // a) Processar Itens sem dedução de stock (apenas validação e criação de reserva)
                    var (success, result) = await ProcessDeliveryItems(
                        delivery,
                        dto.ItemsToDeliver,
                        newScheduledDate,
                        null, // newMovement = null -> SEM DEDUÇÃO
                        lotsToUpdate);

                    if (!success)
                    {
                        await transaction.RollbackAsync();
                        return (false, result);
                    }

                    // b) Atualizar Data e Nota
                    delivery.ScheduledDate = newScheduledDate;
                    delivery.Note = dto.Note ?? delivery.Note;
                }


                // 4. Finalizar transação
                if (newMovement != null)
                {
                    _dbContext.ProductLots.UpdateRange(lotsToUpdate);
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, new Resposta($"Entrega ID {deliveryId} atualizada para o status '{newStatus}' com sucesso."));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, new Resposta("Ocorreu um erro interno ao processar a atualização da entrega."));
            }
        }


        // ====================================================================
        // MÉTODO PÚBLICO: Eliminar entrega existente (apenas agendadas)
        // ====================================================================
        public async Task<(bool success, Resposta? response)> DeleteDelivery(int deliveryID)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var delivery = await _dbContext.Deliveries
                    .Include(d => d.DeliveryItems)
                    .FirstOrDefaultAsync(d => d.Id == deliveryID);

                if (delivery == null)
                {
                    return (false, new Resposta($"Entrega com ID {deliveryID} não encontrada."));
                }

                // Só permite eliminar entregas com estado "Agendada"
                if (delivery.StatusId != (int)Enums.DeliveryStatus.Agendada)
                {
                    return (false, new Resposta($"Apenas entregas com estado 'Agendada' podem ser eliminadas. Estado atual: '{(Enums.DeliveryStatus)delivery.StatusId}'."));
                }

                // Remover itens associados
                if (delivery.DeliveryItems.Any())
                {
                    _dbContext.DeliveryItems.RemoveRange(delivery.DeliveryItems);
                }

                // Remover a entrega
                _dbContext.Deliveries.Remove(delivery);

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, new Resposta($"Entrega ID {deliveryID} eliminada com sucesso."));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, new Resposta("Ocorreu um erro interno ao eliminar a entrega."));
            }
        }

    }
}