using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using sasipca_API.DBModels;
using sasipca_API.Dtos;
using sasipca_API.Services.Interfaces;
using static sasipca_API.Enumerators.Enums;

namespace sasipca_API.Services
{
    public class TemplateGeneratorService : ITemplateGeneratorService
    {
        private readonly SasipcaContext _context;
        private Dictionary<int, string>? _movementTypesCache;

        // Cores do template original
        private static readonly string PrimaryGreen = "#1a5f3c";
        private static readonly string LightGray = "#f5f5f5";
        private static readonly string DarkGray = "#333";
        private static readonly string BorderGray = "#ddd";

        public TemplateGeneratorService(SasipcaContext context)
        {
            _context = context;
            // Configuração de licença obrigatória para QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateReportPdf<T>(T data, ReportTypesEnum type, ReportRequestDTO request, string reportTypeName)
        {
            _movementTypesCache = null;
            var filters = request.Filters as ReportFiltersDTO;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.PageColor(Colors.White);

                    // Header
                    page.Header().Element(c => ComposeHeader(c, reportTypeName));

                    // Content
                    page.Content().Element(c => ComposeContent(c, data, type, filters));

                    // Footer
                    page.Footer().AlignRight().Text(text =>
                    {
                        text.Span("Documento gerado automaticamente. ").FontSize(8).FontColor(Colors.Grey.Medium);
                        text.Span("Página ").FontSize(8).FontColor(Colors.Grey.Medium);
                        text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeHeader(IContainer container, string reportTypeName)
        {
            container.Background(PrimaryGreen).Padding(30).Column(column =>
            {
                column.Item().Text($"Relatório de {reportTypeName}")
                    .Style(TextStyle.Default.FontSize(22).Bold().FontColor(Colors.White));

                column.Item().PaddingTop(8).Text($"Gerado em: {DateTime.Now:dd-MM-yyyy HH:mm}")
                    .Style(TextStyle.Default.FontSize(10).FontColor(Colors.White));
            });
        }

        private void ComposeContent<T>(IContainer container, T data, ReportTypesEnum type, ReportFiltersDTO? filters)
        {
            container.Column(column =>
            {
                column.Item().Element(c => ComposeFilters(c, type, filters));

                if (type == ReportTypesEnum.MovementDetails && data is List<VMovHistoryDetail> details && details.Any())
                {
                    column.Item().PaddingTop(15).Element(c => ComposeMovementDetailsHeader(c, details.First()));
                }

                column.Item().PaddingTop(15).Element(c => ComposeDataTable(c, data, type));
            });
        }

        private void ComposeFilters(IContainer container, ReportTypesEnum type, ReportFiltersDTO? filters)
        {
            container.Background(LightGray).Padding(20).Column(column =>
            {
                column.Item().Text("Filtros Aplicados:")
                    .Style(TextStyle.Default.FontSize(11).Bold().FontColor(PrimaryGreen));

                column.Item().PaddingTop(10).Text(text =>
                {
                    string dateFrom = filters?.DateFrom?.ToString("dd-MM-yyyy") ?? "Todos";
                    string dateTo = filters?.DateTo?.ToString("dd-MM-yyyy") ?? "Todos";

                    text.Span("Período: ").FontSize(9);
                    text.Span($"{dateFrom} a {dateTo}").Style(TextStyle.Default.FontSize(9).Bold().FontColor(PrimaryGreen));
                });

                if (type == ReportTypesEnum.DeliveryHeaders)
                {
                    column.Item().PaddingTop(5).Text(text =>
                    {
                        string status = filters?.DeliveryStatus?.ToString() ?? "Todos";
                        string beneficiary = filters?.BeneficiaryId?.ToString() ?? "Todos";

                        text.Span("Status: ").FontSize(9);
                        text.Span(status).Style(TextStyle.Default.FontSize(9).Bold().FontColor(PrimaryGreen));
                        text.Span(" | Beneficiário ID: ").FontSize(9);
                        text.Span(beneficiary).Style(TextStyle.Default.FontSize(9).Bold().FontColor(PrimaryGreen));
                    });
                }
            });
        }

        private void ComposeMovementDetailsHeader(IContainer container, VMovHistoryDetail header)
        {
            string typeName = GetMovementTypeName(header.MovementTypeId);

            container.Background(LightGray).Padding(20).Column(column =>
            {
                column.Item().Text("Detalhes do Movimento:")
                    .Style(TextStyle.Default.FontSize(11).Bold().FontColor(PrimaryGreen));

                column.Item().PaddingTop(10).Text(text =>
                {
                    text.Span("Tipo: ").FontSize(9);
                    text.Span(typeName).Style(TextStyle.Default.FontSize(9).Bold().FontColor(PrimaryGreen));
                    text.Span(" | Data: ").FontSize(9);
                    text.Span(header.MovementDate.ToString("yyyy-MM-dd HH:mm")).Style(TextStyle.Default.FontSize(9).Bold().FontColor(PrimaryGreen));
                });

                column.Item().PaddingTop(5).Text(text =>
                {
                    text.Span("Utilizador: ").FontSize(9);
                    text.Span(header.UserName).Style(TextStyle.Default.FontSize(9).Bold().FontColor(PrimaryGreen));
                    text.Span(" | Nota: ").FontSize(9);
                    text.Span(header.MovementNote ?? "N/A").Style(TextStyle.Default.FontSize(9).Bold().FontColor(PrimaryGreen));
                });
            });
        }

        private void ComposeDataTable<T>(IContainer container, T data, ReportTypesEnum type)
        {
            container.Table(table =>
            {
                if (type == ReportTypesEnum.MovementHeaders && data is List<VMovHistory> history)
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1.5f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Data");
                        header.Cell().Element(CellStyle).Text("Tipo");
                        header.Cell().Element(CellStyle).Text("Utilizador");
                        header.Cell().Element(CellStyle).Text("Nota");
                        header.Cell().Element(CellStyle).AlignRight().Text("Qtd Total");

                        static IContainer CellStyle(IContainer container) => container.Background(PrimaryGreen).Padding(12).DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White).FontSize(9));
                    });

                    foreach (var h in history)
                    {
                        table.Cell().Element(RowStyle).Text(h.MovementDate.ToString("yyyy-MM-dd HH:mm"));
                        table.Cell().Element(RowStyle).Text(GetMovementTypeName(h.MovementTypeId));
                        table.Cell().Element(RowStyle).Text(h.UserName);
                        table.Cell().Element(RowStyle).Text(h.MovementNote ?? "-");
                        table.Cell().Element(RowStyle).AlignRight().Text(h.TotalQuantityAffected.ToString());
                    }
                }
                else if (type == ReportTypesEnum.DeliveryHeaders && data is List<VDelivery> deliveries)
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(2.5f);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Data Agendada");
                        header.Cell().Element(CellStyle).Text("Status");
                        header.Cell().Element(CellStyle).Text("Beneficiário");
                        header.Cell().Element(CellStyle).Text("Utilizador");
                        header.Cell().Element(CellStyle).Text("Nota");

                        static IContainer CellStyle(IContainer container) => container.Background(PrimaryGreen).Padding(12).DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White).FontSize(9));
                    });

                    foreach (var d in deliveries)
                    {
                        table.Cell().Element(RowStyle).Text(d.ScheduledDate.ToString("yyyy-MM-dd"));
                        table.Cell().Element(RowStyle).Text(d.StatusId.ToString());
                        table.Cell().Element(RowStyle).Text(d.BeneficiaryName);
                        table.Cell().Element(RowStyle).Text(d.UserName);
                        table.Cell().Element(RowStyle).Text(d.Note ?? "-");
                    }
                }
                else if (type == ReportTypesEnum.MovementDetails && data is List<VMovHistoryDetail> details)
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2.5f);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1.5f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Código de Barras");
                        header.Cell().Element(CellStyle).Text("Produto");
                        header.Cell().Element(CellStyle).Text("Data de Validade");
                        header.Cell().Element(CellStyle).AlignRight().Text("Qtd");

                        static IContainer CellStyle(IContainer container) => container.Background(PrimaryGreen).Padding(12).DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White).FontSize(9));
                    });

                    foreach (var d in details)
                    {
                        table.Cell().Element(RowStyle).Text(d.ProductBarcode);
                        table.Cell().Element(RowStyle).Text(d.ProductName);
                        table.Cell().Element(RowStyle).Text(d.GroupExpiryDate.ToString("dd-MM-yyyy"));
                        table.Cell().Element(RowStyle).AlignRight().Text(d.ItemQuantityAffected.ToString());
                    }
                }

                static IContainer RowStyle(IContainer container) => container.BorderBottom(1).BorderColor(BorderGray).Padding(10).DefaultTextStyle(x => x.FontSize(9).FontColor(DarkGray));
            });
        }

        private string GetMovementTypeName(int typeId)
        {
            if (_movementTypesCache == null)
            {
                try { _movementTypesCache = _context.MovementTypes.ToDictionary(k => k.Id, v => v.Type); }
                catch { _movementTypesCache = new Dictionary<int, string>(); }
            }
            return _movementTypesCache.ContainsKey(typeId) ? _movementTypesCache[typeId] : typeId.ToString();
        }
    }
}