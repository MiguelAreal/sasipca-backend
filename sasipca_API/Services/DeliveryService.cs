using FluentAssertions;
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
        private readonly IJobSchedulerService _jobScheduler;

        public DeliveryService(SasipcaContext dbContext, IJobSchedulerService jobScheduler)
        {
            _dbContext = dbContext;
            _jobScheduler = jobScheduler;
        }

        // ====================================================================
        // FUNÇÃO PRIVADA ABSTRAÍDA: PROCESSA ITENS, VALIDA STOCK E VALIDADE
        // ====================================================================
        /// <summary>
        /// Processa a lista de itens de entrega: valida, cria DeliveryItems, e opcionalmente
        /// deduz o stock e cria os MovementItems.
        /// </summary>
        /// <param name="delivery">A instância da Delivery atual.</param>
        /// <param name="itemDtos">Os itens a serem processados.</param>
        /// <param name="scheduledDate">A data agendada para validação de expiração.</param>
        /// <param name="newMovement">A instância do Movement para log (ou null se não houver dedução).</param>
        /// <param name="groupsToUpdate">Lista para acumular grupos a serem atualizados (dedução).</param>
        /// <returns>Tuplo com sucesso e Resposta (se falhar).</returns>
        private async Task<(bool success, Resposta? response)> ProcessDeliveryItems(
            Delivery delivery,
            List<DeliveryItemDTO> itemDtos,
            DateOnly scheduledDate,
            Movement? newMovement,
            List<ProductGroup> groupsToUpdate)
        {
            var isDeductingStock = newMovement != null;
            var isScheduling = delivery.StatusId == (int)Enums.DeliveryStatus.Agendada;

            foreach (var itemDto in itemDtos)
            {
                // Encontrar o grupo pelo groupId
                var productGroup = await _dbContext.ProductGroups
                    .Include(pl => pl.BarcodeNavigation)
                    .FirstOrDefaultAsync(pl => pl.Id == itemDto.groupId);

                if (productGroup == null)
                {
                    return (false, new Resposta($"Grupo do produto '{itemDto.Barcode}' não encontrado."));
                }

                // 1. VALIDAÇÃO DE EXPIRAÇÃO (Apenas para agendamento)
                if (isScheduling)
                {
                    // Bloquear se a data de validade for ANTES ou NO DIA da entrega agendada.
                    if (productGroup.ExpiryDate <= scheduledDate)
                    {
                        return (false, new Resposta($"Agendamento bloqueado. O produto '{productGroup.BarcodeNavigation.Name}' expira antes ou no dia da entrega agendada."));
                    }
                }

                // 2. VALIDAÇÃO DE STOCK DISPONÍVEL (Sempre necessária para reserva ou saída)

                // 2.1. Obter a quantidade TOTAL reservada (excluindo a entrega atual, se for atualização)
                var reservedQuantity = await _dbContext.DeliveryItems
                    .Where(di => di.ProductGroupId == productGroup.Id
                            && di.Delivery.StatusId == (int)Enums.DeliveryStatus.Agendada
                            && di.DeliveryId != delivery.Id) // Excluir reservas desta mesma entrega se for UPDATE
                    .SumAsync(di => di.Quantity);

                var availableStock = productGroup.Quantity - reservedQuantity;

                // 2.2. Validação: Stock Disponível vs. Quantidade Solicitada
                if (availableStock < itemDto.Quantity)
                {
                    return (false, new Resposta($"Stock insuficiente para reserva/saída. Produto '{productGroup.BarcodeNavigation.Name}' (Stock Disponível: {availableStock}, Quantidade solicitada: {itemDto.Quantity}."));
                }

                // 3. CRIAÇÃO DO DELIVERY ITEM
                delivery.DeliveryItems.Add(new DeliveryItem
                {
                    DeliveryId = delivery.Id,
                    ProductGroupId = productGroup.Id,
                    Quantity = (int)itemDto.Quantity
                });

                // 4. DEDUÇÃO DE STOCK e LOG (Apenas se for Saída Imediata)
                if (isDeductingStock)
                {
                    productGroup.Quantity -= (int)itemDto.Quantity;
                    groupsToUpdate.Add(productGroup); // Marcar para atualização em lote

                    if (newMovement != null)
                    {
                        newMovement.MovementItems.Add(new MovementItem
                        {
                            ProductGroup = productGroup,
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
                var groupsToUpdate = new List<ProductGroup>();

                // Se for para deduzir stock (é uma saída espontânea)
                if (deductStock)
                {
                    newMovement = new Movement
                    {
                        UserId = userId,
                        MovementTypeId = (int)Enums.MovementTypes.Saida,
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
                    groupsToUpdate);

                if (!success)
                {
                    await transaction.RollbackAsync();
                    return (false, result);
                }

                // 5. Finalizar transação
                if (deductStock && newMovement != null)
                {
                    _dbContext.ProductGroups.UpdateRange(groupsToUpdate);
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                if (initialStatus == Enums.DeliveryStatus.Agendada)
                {
                    _jobScheduler.ScheduleDeliveryCheck(newDelivery.Id, dto.ScheduledDate);
                }


                var message = initialStatus == Enums.DeliveryStatus.Entregue
                    ? $"Entrega imediata registada e stock deduzido."
                    : $"Entrega agendada com sucesso.";

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

                if (delivery == null) return (false, new Resposta($"Entrega com ID {deliveryId} não encontrada."));

                // 1. VALIDAÇÃO DE ESTADO
                if (delivery.StatusId == (int)Enums.DeliveryStatus.Entregue || delivery.StatusId == (int)Enums.DeliveryStatus.Cancelada)
                {
                    return (false, new Resposta($"A entrega já está em estado final e não pode ser alterada."));
                }

                var newStatus = dto.NewStatusId;
                var newScheduledDate = dto.ScheduledDate ?? delivery.ScheduledDate;
                var groupsToUpdate = new List<ProductGroup>();
                Movement? newMovement = null;

                // 2. PROCESSAMENTO
                if (newStatus == (int)Enums.DeliveryStatus.Cancelada)
                {
                    delivery.StatusId = (int)Enums.DeliveryStatus.Cancelada;
                    delivery.Note = dto.Note ?? delivery.Note;
                }
                else if (newStatus == (int)Enums.DeliveryStatus.Entregue)
                {
                    // --- PASSAR PARA ENTREGUE (Usa itens existentes na BD) ---
                    newMovement = new Movement
                    {
                        UserId = userId,
                        MovementTypeId = (int)Enums.MovementTypes.Saida,
                        Delivery = delivery,
                        Note = dto.Note ?? delivery.Note
                    };
                    _dbContext.Movements.Add(newMovement);

                    // Convertemos os itens que já estão na entrega para o formato que a função de processamento aceita
                    var existingItems = delivery.DeliveryItems.Select(di => new DeliveryItemDTO
                    {
                        groupId = di.ProductGroupId,
                        Quantity = di.Quantity
                    }).ToList();

                    // Importante: Como vamos processar os mesmos itens para dar saída, 
                    // limpamos a lista da relação para o ProcessDeliveryItems não tentar adicionar duplicados na BD
                    var itemsBackup = delivery.DeliveryItems.ToList();
                    delivery.DeliveryItems.Clear();

                    var (success, result) = await ProcessDeliveryItems(delivery, existingItems, newScheduledDate, newMovement, groupsToUpdate);
                    if (!success) return (false, result);

                    delivery.StatusId = (int)Enums.DeliveryStatus.Entregue;
                }
                else // newStatus == Agendada
                {
                    // --- ATUALIZAR AGENDAMENTO (Pode trocar itens) ---

                    // Aqui sim, apagamos os antigos para reintroduzir os novos do DTO
                    if (delivery.DeliveryItems.Any())
                    {
                        _dbContext.DeliveryItems.RemoveRange(delivery.DeliveryItems);
                        delivery.DeliveryItems.Clear();
                    }

                    var (success, result) = await ProcessDeliveryItems(delivery, dto.ItemsToDeliver, newScheduledDate, null, groupsToUpdate);
                    if (!success) return (false, result);

                    _jobScheduler.ScheduleDeliveryCheck(delivery.Id, newScheduledDate);
                    delivery.StatusId = (int)Enums.DeliveryStatus.Agendada;
                }

                delivery.ScheduledDate = newScheduledDate;
                delivery.Note = dto.Note ?? delivery.Note;

                if (newMovement != null && groupsToUpdate.Any())
                {
                    _dbContext.ProductGroups.UpdateRange(groupsToUpdate);
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, new Resposta($"Entrega {deliveryId} atualizada para '{(Enums.DeliveryStatus)newStatus}'."));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, new Resposta($"Erro: {ex.Message}"));
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