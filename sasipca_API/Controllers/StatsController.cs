using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sasipca_API.Attributes;
using sasipca_API.DBModels;
using sasipca_API.Dtos;
using sasipca_API.Enumerators;
using static sasipca_API.Enumerators.Enums;

namespace sasipca_API.Controllers
{
    [Route("api/stats")]
    [ApiController]
    public class StatsController : ControllerBase
    {
        private readonly SasipcaContext _context;

        public StatsController(SasipcaContext context)
        {
            _context = context;
        }

        // ---------------------------------------------------------
        // 1. DETAILED DASHBOARD SUMMARY (KPIs)
        // ---------------------------------------------------------
        [HttpGet("summary")]
        [AuthorizeRole(UserRole.Admin)]
        public async Task<ActionResult<DashboardSummaryDTO>> GetSummary([FromQuery] DateRangeFilterDTO filter)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            // 1. Stock Total (Geral)
            var totalStock = await _context.ProductGroups.SumAsync(pg => pg.Quantity);

            // 2. Entregas Pendentes (Geral - Mantido)
            var pendingDeliveries = await _context.Deliveries
                .CountAsync(d => d.StatusId == (int)Enums.DeliveryStatus.Agendada);

            // 3. Quantidade de Stock Expirado
            var expiredStock = await _context.ProductGroups
                .Where(pg => pg.ExpiryDate < today)
                .SumAsync(pg => pg.Quantity);

            // 4. Novos Beneficiários no Período
            var beneficiariesQuery = _context.Beneficiaries.AsQueryable();

            if (filter.DateFrom.HasValue)
            {
                var dtFrom = filter.DateFrom.Value.ToDateTime(TimeOnly.MinValue);
                beneficiariesQuery = beneficiariesQuery.Where(b => b.CreatedAt >= dtFrom);
            }

            if (filter.DateTo.HasValue)
            {
                var dtTo = filter.DateTo.Value.ToDateTime(TimeOnly.MaxValue);
                beneficiariesQuery = beneficiariesQuery.Where(b => b.CreatedAt <= dtTo);
            }

            var newBeneficiaries = await beneficiariesQuery.CountAsync();

            return Ok(new DashboardSummaryDTO
            {
                TotalProductsInStock = totalStock,
                PendingDeliveriesCount = pendingDeliveries,
                ExpiredStockQuantity = expiredStock,
                NewBeneficiariesCount = newBeneficiaries
            });
        }

        // ---------------------------------------------------------
        // 2. FLUXO DE STOCK (Entradas vs Saídas - Gráfico Linhas)
        // ---------------------------------------------------------
        [HttpGet("movements-flow")]
        [AuthorizeRole(UserRole.Admin)]
        public async Task<ActionResult<List<ChartDataPoint>>> GetMovementsFlow([FromQuery] DateRangeFilterDTO filter)
        {
            var query = _context.VStatsDailymovements.AsQueryable();

            if (filter.DateFrom.HasValue)
            {
                var dtFrom = filter.DateFrom.Value.ToDateTime(TimeOnly.MinValue);
                query = query.Where(v => v.MovementDate >= dtFrom);
            }

            if (filter.DateTo.HasValue)
            {
                var dtTo = filter.DateTo.Value.ToDateTime(TimeOnly.MaxValue);
                query = query.Where(v => v.MovementDate <= dtTo);
            }

            var rawData = await query
                .GroupBy(v => new { v.MovementDate, v.MovementType })
                .Select(g => new
                {
                    g.Key.MovementDate,
                    g.Key.MovementType,
                    Total = g.Sum(x => x.TotalQuantity)
                })
                .OrderBy(x => x.MovementDate)
                .ToListAsync();

            var result = rawData.Select(x => new ChartDataPoint
            {
                Label = x.MovementDate.ToString("yyyy-MM-dd"),
                Series = x.MovementType,
                Value = (double)(x.Total ?? 0)
            }).ToList();

            return Ok(result);
        }

        // ---------------------------------------------------------
        // 3. TOP PRODUTOS (SAÍDAS) - Gráfico Barras
        // ---------------------------------------------------------
        [HttpGet("top-products")]
        [AuthorizeRole(UserRole.Admin)]
        public async Task<ActionResult<List<ChartDataPoint>>> GetTopProducts([FromQuery] DateRangeFilterDTO filter, [FromQuery] int topN = 5)
        {
            var query = _context.VStatsDailymovements
                .Where(v => v.MovementTypeId == (int)Enums.MovementTypes.Saida);

            if (filter.DateFrom.HasValue)
            {
                var dtFrom = filter.DateFrom.Value.ToDateTime(TimeOnly.MinValue);
                query = query.Where(v => v.MovementDate >= dtFrom);
            }

            if (filter.DateTo.HasValue)
            {
                var dtTo = filter.DateTo.Value.ToDateTime(TimeOnly.MaxValue);
                query = query.Where(v => v.MovementDate <= dtTo);
            }

            var rawData = await query
                .GroupBy(v => v.ProductName)
                .Select(g => new
                {
                    ProductName = g.Key,
                    Total = g.Sum(x => x.TotalQuantity)
                })
                .OrderByDescending(x => x.Total)
                .Take(topN)
                .ToListAsync();

            var result = rawData.Select(x => new ChartDataPoint
            {
                Label = x.ProductName ?? "Desconhecido",
                Value = (double)(x.Total ?? 0),
                Series = "Total Saída"
            }).ToList();

            return Ok(result);
        }

        // ---------------------------------------------------------
        // 4. DISTRIBUIÇÃO POR CATEGORIA (CONSOLIDADO) - Donut Chart
        // ---------------------------------------------------------
        // movementTypeId: 1 = Entrada, 2 = Saída
        [HttpGet("categories-distribution")]
        [AuthorizeRole(UserRole.Admin)]
        public async Task<ActionResult<List<ChartDataPoint>>> GetCategoriesDistribution(
            [FromQuery] DateRangeFilterDTO filter,
            [FromQuery] int movementTypeId)
        {
            var query = _context.VStatsDailymovements
                .Where(v => v.MovementTypeId == movementTypeId);

            if (filter.DateFrom.HasValue)
            {
                var dtFrom = filter.DateFrom.Value.ToDateTime(TimeOnly.MinValue);
                query = query.Where(v => v.MovementDate >= dtFrom);
            }

            if (filter.DateTo.HasValue)
            {
                var dtTo = filter.DateTo.Value.ToDateTime(TimeOnly.MaxValue);
                query = query.Where(v => v.MovementDate <= dtTo);
            }

            var rawData = await query
                .GroupBy(v => v.CategoryName)
                .Select(g => new
                {
                    CategoryName = g.Key,
                    Total = g.Sum(x => x.TotalQuantity)
                })
                .OrderByDescending(x => x.Total)
                .ToListAsync();

            var result = rawData.Select(x => new ChartDataPoint
            {
                Label = x.CategoryName ?? "Sem Categoria",
                Value = (double)(x.Total ?? 0)
            }).ToList();

            return Ok(result);
        }

        // ---------------------------------------------------------
        // 5. RESUMO MENSAL (PARA A HOMEPAGE) - RESTAURADO
        // ---------------------------------------------------------
        [HttpGet("monthly-summary")]
        [AuthorizeRole(UserRole.Admin)]
        public async Task<ActionResult<MonthlySummaryDTO>> GetMonthlySummary([FromQuery] int month, [FromQuery] int year)
        {
            // Datas limite do mês
            var startDate = new DateOnly(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // 1. Entregas Pendentes (Agendadas para este mês)
            var pending = await _context.Deliveries
                .CountAsync(d => d.StatusId == (int)Enums.DeliveryStatus.Agendada
                                 && d.ScheduledDate >= startDate
                                 && d.ScheduledDate <= endDate);

            // 2. Entregas Realizadas (Entregues neste mês)
            var realized = await _context.Deliveries
                .CountAsync(d => d.StatusId == (int)Enums.DeliveryStatus.Entregue
                                 && d.ScheduledDate >= startDate
                                 && d.ScheduledDate <= endDate);

            // 3. Doações/Receções (Entradas de Stock neste mês)
            var donationsReceived = await _context.Movements
                .CountAsync(m => m.MovementTypeId == (int)Enums.MovementTypes.Entrada
                                 && DateOnly.FromDateTime(m.CreatedAt) >= startDate
                                 && DateOnly.FromDateTime(m.CreatedAt) <= endDate);

            return Ok(new MonthlySummaryDTO
            {
                Month = month,
                Year = year,
                PendingDeliveries = pending,
                RealizedDeliveries = realized,
                DonationsReceived = donationsReceived
            });
        }

        // ---------------------------------------------------------
        // 6. STOCK TOTAL POR CATEGORIA (PÚBLICO)
        // ---------------------------------------------------------
        [HttpGet("stock-by-category")]
        [AllowAnonymous]
        public async Task<ActionResult<List<ChartDataPoint>>> GetStockByCategory()
        {
            var data = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductGroups)
                .GroupBy(p => p.Category.Type)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key ?? "Sem Categoria",
                    Value = g.Sum(p => p.ProductGroups.Sum(pg => pg.Quantity)),
                    Series = "Stock Atual"
                })
                .Where(x => x.Value > 0)
                .OrderByDescending(x => x.Value)
                .ToListAsync();

            return Ok(data);
        }
    }
}