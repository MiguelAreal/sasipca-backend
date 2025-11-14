using sasipca_API.DBModels;
using sasipca_API.Dtos;
using sasipca_API.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static sasipca_API.Enumerators.Enums;

namespace sasipca_API.Services
{
    public class TemplateGeneratorService : ITemplateGeneratorService
    {
        private readonly string _basePath;

        public TemplateGeneratorService()
        {
            _basePath = Path.Combine(Directory.GetCurrentDirectory(), "ReportTemplates");
        }

        public string GenerateReportHtml<T>(T data, ReportTypesEnum type, ReportRequestDTO request, string reportTypeName)
        {
            // 1. Carregar Template Base e CSS
            const string templateFileName = "ReportTemplate.html";
            var templatePath = Path.Combine(_basePath, templateFileName);

            string htmlTemplate = File.ReadAllText(templatePath, Encoding.UTF8);

            // Obter filtros
            var filters = request.Filters as ReportFiltersDTO;

            // --- 2. Gerar Conteúdo Condicional ---
            string dynamicContent = GenerateDynamicReportBody(data, type, filters);


            // Substituições de metadados
            htmlTemplate = htmlTemplate.Replace("{report_title}", $"Relatório de {reportTypeName}");
            htmlTemplate = htmlTemplate.Replace("{generation_date}", DateTime.Now.ToString("dd-MM-yyyy HH:mm"));

            // Injeta o corpo gerado
            return htmlTemplate.Replace("{report_content}", dynamicContent);
        }

        private string GenerateDynamicReportBody<T>(T data, ReportTypesEnum type, ReportFiltersDTO? filters)
        {
            var sb = new StringBuilder();

            // --- PARTE 1: FILTROS E DETALHES GERAIS ---
            sb.AppendLine("<div class='filters'>");
            sb.AppendLine("<h4>Filtros Aplicados:</h4>");

            string dateFrom = filters?.DateFrom?.ToString("dd-MM-yyyy") ?? "Todos";
            string dateTo = filters?.DateTo?.ToString("dd-MM-yyyy") ?? "Todos";
            sb.AppendLine($"<p>Período: <strong>{dateFrom}</strong> a <strong>{dateTo}</strong></p>");

            if (type == ReportTypesEnum.DeliveryHeaders)
            {
                string status = filters?.DeliveryStatus?.ToString() ?? "Todos";
                string beneficiary = filters?.BeneficiaryId?.ToString() ?? "Todos";
                sb.AppendLine($"<p>Status: <strong>{status}</strong> | Beneficiário ID: <strong>{beneficiary}</strong></p>");
            }

            // Lógica de detalhes específicos (MovementDetails - Cabeçalho do movimento)
            if (type == ReportTypesEnum.MovementDetails && data is List<VMovHistoryDetail> details && details.Any()) // <<-- CORRIGIDO
            {
                var header = details.First();
                sb.AppendLine("<h4>Detalhes do Movimento:</h4>");
                sb.AppendLine($"<p>Tipo: <strong>{header.MovementTypeId}</strong> | Data: <strong>{header.MovementDate:yyyy-MM-dd HH:mm}</strong></p>");
                sb.AppendLine($"<p>Utilizador: <strong>{header.UserName}</strong> | Nota: <strong>{header.MovementNote ?? "N/A"}</strong></p>");

                if (header.DeliveryId.HasValue)
                {
                    sb.AppendLine($"<p>ENTREGA (Delivery ID: {header.DeliveryId})</p>");
                }
            }

            sb.AppendLine("</div>");

            // --- PARTE 2: TABELA DE DADOS ---

            sb.AppendLine("<table class='report-table'>");
            sb.AppendLine("<thead>");

            if (type == ReportTypesEnum.MovementHeaders && data is List<VMovHistory> history)
            {
                sb.AppendLine("<tr><th>Data</th><th>Tipo</th><th>Utilizador</th><th>Nota</th><th class='align-right'>Qtd Total</th></tr>");
                sb.AppendLine("</thead><tbody>");
                foreach (var h in history)
                {
                    sb.AppendLine($"<tr><td>{h.MovementDate:yyyy-MM-dd HH:mm}</td><td>{h.MovementTypeId}</td><td>{h.UserName}</td><td>{h.MovementNote ?? "-"}</td><td class='align-right'>{h.TotalQuantityAffected}</td></tr>");
                }
            }
            else if (type == ReportTypesEnum.DeliveryHeaders && data is List<VDelivery> deliveries)
            {
                sb.AppendLine("<tr><th>Data Agendada</th><th>Status</th><th>Beneficiário</th><th>Utilizador</th><th>Nota</th></tr>");
                sb.AppendLine("</thead><tbody>");
                foreach (var d in deliveries)
                {
                    sb.AppendLine($"<tr><td>{d.ScheduledDate:yyyy-MM-dd}</td><td>{d.StatusId}</td><td>{d.BeneficiaryName}</td><td>{d.UserName}</td><td>{d.Note ?? "-"}</td></tr>");
                }
            }
            else if (type == ReportTypesEnum.MovementDetails && data is List<VMovHistoryDetail> movdetails)
            {
                sb.AppendLine("<tr><th>Código de Barras</th><th>Produto</th><th>Lote</th><th>Validade</th><th class='align-right'>Qtd</th></tr>");
                sb.AppendLine("</thead><tbody>");
                foreach (var d in movdetails)
                {
                    sb.AppendLine($"<tr><td>{d.ProductBarcode}</td><td>{d.ProductName}</td><td>{d.ProductLotNumber}</td><td>{d.LotExpiryDate.ToString("dd-MM-yyyy")}</td><td class='align-right'>{d.ItemQuantityAffected}</td></tr>");
                }
            }

            sb.AppendLine("</tbody></table>");

            return sb.ToString();
        }
    }
}