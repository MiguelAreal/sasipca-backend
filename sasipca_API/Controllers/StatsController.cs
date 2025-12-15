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
        // 1. DASHBOARD SUMMARY (KPIs)
        // ---------------------------------------------------------
        [HttpGet("summary")]
        [AuthorizeRole(UserRole.Admin)]
        public async Task<ActionResult<DashboardSummaryDTO>> GetSummary()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var lastMonth = today.AddDays(-30);

            // Total de itens em stock
            var totalStock = await _context.ProductGroups.SumAsync(pg => pg.Quantity);

            // Produtos com stock baixo
            var lowStock = await _context.Products
                .Select(p => new
                {
                    p.ExpNotif,
                    TotalQuantity = p.ProductGroups.Sum(pg => pg.Quantity)
                })
                .Where(x => x.TotalQuantity <= (x.ExpNotif ?? 5))
                .CountAsync();

            // Entregas Pendentes
            var pendingDeliveries = await _context.Deliveries
                .CountAsync(d => d.StatusId == (int)Enums.DeliveryStatus.Agendada);

            // Beneficiários Ativos
            var activeBeneficiaries = await _context.Deliveries
                .Where(d => d.StatusId == (int)Enums.DeliveryStatus.Entregue
                            && d.ScheduledDate >= lastMonth)
                .Select(d => d.BeneficiaryId)
                .Distinct()
                .CountAsync();

            return Ok(new DashboardSummaryDTO
            {
                TotalProductsInStock = totalStock,
                LowStockCount = lowStock,
                PendingDeliveriesCount = pendingDeliveries,
                ActiveBeneficiariesCount = activeBeneficiaries
            });
        }

        // ---------------------------------------------------------
        // 2. FLUXO DE STOCK (Entradas vs Saídas) - Gráfico de Linhas
        // ---------------------------------------------------------
        [HttpGet("movements-flow")]
        [AuthorizeRole(UserRole.Admin)]
        public async Task<ActionResult<List<ChartDataPoint>>> GetMovementsFlow([FromQuery] DateRangeFilterDTO filter)
        {
            var query = _context.VStatsDailymovements.AsQueryable();

            // CORREÇÃO: Converter DateOnly para DateTime para comparação com a base de dados
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

            // Agrupar por Data e Tipo
            // Nota: Para evitar erro de tradução LINQ, fazemos a projeção final em memória (ToList) se necessário,
            // mas aqui o GroupBy simples deve funcionar se a View já tiver MovementDate como DateTime.
            var rawData = await query
                .GroupBy(v => new { v.MovementDate, v.MovementType })
                .Select(g => new
                {
                    g.Key.MovementDate,
                    g.Key.MovementType,
                    Total = g.Sum(x => x.TotalQuantity) // Sum retorna int/long/decimal?
                })
                .OrderBy(x => x.MovementDate)
                .ToListAsync();

            // Mapeamento para DTO final (Cast seguro para double)
            var result = rawData.Select(x => new ChartDataPoint
            {
                Label = x.MovementDate.ToString("yyyy-MM-dd"), // Formatamos a data aqui
                Series = x.MovementType,
                Value = (double)(x.Total ?? 0) // Cast explícito e tratamento de nulo
            }).ToList();

            return Ok(result);
        }

        // ---------------------------------------------------------
        // 3. TOP PRODUTOS MAIS ENTREGUES - Gráfico de Barras/Pie
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
                Value = (double)(x.Total ?? 0), // Cast explícito
                Series = "Total Saída"
            }).ToList();

            return Ok(result);
        }

        // ---------------------------------------------------------
        // 4. ENTREGAS POR CATEGORIA - Gráfico de Donut
        // ---------------------------------------------------------
        [HttpGet("categories-distribution")]
        [AuthorizeRole(UserRole.Admin)]
        public async Task<ActionResult<List<ChartDataPoint>>> GetCategoriesDistribution([FromQuery] DateRangeFilterDTO filter)
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
                Value = (double)(x.Total ?? 0) // Cast explícito
            }).ToList();

            return Ok(result);
        }

        // ---------------------------------------------------------
        // 5. RESUMO MENSAL (PARA A HOMEPAGE)
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
            // Nota: Usamos ScheduledDate como data de entrega efetiva ou devíamos ter um DeliveredDate? 
            // Assumindo ScheduledDate para simplificar a query na View atual.
            var realized = await _context.Deliveries
                .CountAsync(d => d.StatusId == (int)Enums.DeliveryStatus.Entregue
                                 && d.ScheduledDate >= startDate
                                 && d.ScheduledDate <= endDate);

            // 3. Doações Feitas (Entradas de Stock / Receipts neste mês)
            // "Doações Feitas" geralmente refere-se ao que a instituição recebeu (Entradas).
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
            // Usamos a tabela Products e ProductGroups para calcular o stock real atual
            var data = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductGroups)
                .GroupBy(p => p.Category.Type) // Agrupa pelo nome da categoria
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key ?? "Sem Categoria",
                    // Soma a quantidade de todos os grupos de todos os produtos dessa categoria
                    Value = g.Sum(p => p.ProductGroups.Sum(pg => pg.Quantity)),
                    Series = "Stock Atual"
                })
                .Where(x => x.Value > 0) // Opcional: Mostra apenas categorias com stock
                .OrderByDescending(x => x.Value)
                .ToListAsync();

            return Ok(data);
        }
    }
}