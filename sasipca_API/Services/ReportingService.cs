using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sasipca_API.DBModels;
using sasipca_API.Dtos;
using sasipca_API.Models;
using sasipca_API.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static sasipca_API.Dtos.ReportRequestDTO;
using static sasipca_API.Enumerators.Enums;
using WkHtmlToPdfDotNet.Contracts;
using WkHtmlToPdfDotNet;
using System.IO;

namespace sasipca_API.Services
{
    // ====================================================================
    // SERVIÇO PRINCIPAL DE RELATÓRIOS (HTML-to-PDF)
    // ====================================================================
    public class ReportingService : IReportingService
    {
        private readonly SasipcaContext _dbContext;
        private readonly ITemplateGeneratorService _templateGeneratorService;
        private readonly IConverter _pdfConverter;

        public ReportingService(SasipcaContext dbContext, ITemplateGeneratorService templateGeneratorService, IConverter pdfConverter)
        {
            _dbContext = dbContext;
            _templateGeneratorService = templateGeneratorService;
            _pdfConverter = pdfConverter;
        }

        // --------------------------------------------------------------------
        // MÉTODO PRINCIPAL DA INTERFACE (Agora guarda na BD e no Disco)
        // --------------------------------------------------------------------
        public async Task<(byte[] fileContent, string mimeType, string fileName, int newReportId)> GenerateReportAsync(ReportRequestDTO request, int creatorId)
        {
            string reportTypeName;
            try
            {
                int reportTypeId = (int)request.Type;
                reportTypeName = await _dbContext.ReportTypes
                    .Where(rt => rt.Id == reportTypeId)
                    .Select(rt => rt.Type)
                    .FirstOrDefaultAsync() ?? request.Type.ToString();
            }
            catch
            {
                reportTypeName = request.Type.ToString();
            }

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileExtension = request.Format == ReportFormat.PDF ? "pdf" : "csv";
            var baseFileName = request.FileName.Replace(" ", "_");
            var finalFileName = $"{baseFileName}_{timestamp}.{fileExtension}";

            var data = await GetFilteredDataAsync(request);

            if (data == null || (data is IEnumerable<object> enumerable && !enumerable.Any()))
            {
                return (Array.Empty<byte>(), "application/octet-stream", finalFileName, 0);
            }

            byte[] fileContent;
            string mimeType;

            if (request.Format == ReportFormat.CSV)
            {
                fileContent = GenerateCsvContent(data, (ReportTypesEnum)request.Type);
                mimeType = "text/csv";
            }
            else // PDF
            {
                fileContent = GeneratePdfContent(data, (ReportTypesEnum)request.Type, request, reportTypeName);
                mimeType = "application/pdf";
            }

            // --- NOVO PASSO: GUARDAR NO DISCO E NA BASE DE DADOS ---

            // 1. Guardar no Disco
            var reportsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Reports");
            if (!Directory.Exists(reportsDirectory))
            {
                Directory.CreateDirectory(reportsDirectory);
            }
            var filePath = Path.Combine(reportsDirectory, finalFileName);
            await File.WriteAllBytesAsync(filePath, fileContent);

            // 2. Guardar na Base de Dados
            var reportEntry = new Report
            {
                Name = finalFileName,
                CreatorId = creatorId,
                ReportType = (int)request.Type
            };

            _dbContext.Reports.Add(reportEntry);
            await _dbContext.SaveChangesAsync();

            // Retorna o conteúdo e o novo ID do relatório
            return (fileContent, mimeType, finalFileName, reportEntry.Id);
        }


        // --------------------------------------------------------------------
        // NOVO MÉTODO: LISTAGEM DE RELATÓRIOS GERADOS (Mantido)
        // --------------------------------------------------------------------
        public async Task<IEnumerable<ReportGetDTO>> GetGeneratedReportsMetadataAsync(int? reportTypeId = null)
        {
            var query = _dbContext.Reports.AsQueryable();

            if (reportTypeId.HasValue && reportTypeId.Value > 0)
            {
                query = query.Where(r => r.ReportType == reportTypeId.Value);
            }

            var reports = await query
                .Include(r => r.Creator)
                .Include(r => r.ReportTypeNavigation)
                .Select(r => new ReportGetDTO
                {
                    Id = r.Id,
                    Name = r.Name,
                    CreatorName = r.Creator.Name,
                    ReportTypeId = r.ReportType,
                    ReportTypeName = r.ReportTypeNavigation.Type,
                    CreatedAt = r.CreatedAt
                })
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reports;
        }

        // --------------------------------------------------------------------
        // NOVO MÉTODO: OBTER ARQUIVO DO DISCO (Mantido)
        // --------------------------------------------------------------------
        public async Task<(byte[] fileContent, string mimeType, string fileName)> GetReportFileAsync(int reportId)
        {
            var reportEntry = await _dbContext.Reports.FindAsync(reportId);

            if (reportEntry == null)
            {
                throw new KeyNotFoundException($"Relatório com ID {reportId} não encontrado na base de dados.");
            }

            var fileName = reportEntry.Name;
            var reportsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Reports");
            var filePath = Path.Combine(reportsDirectory, fileName);

            var mimeType = fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                           ? "application/pdf"
                           : "text/csv";

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"O ficheiro de relatório '{fileName}' não foi encontrado no disco.");
            }

            byte[] fileContent = await File.ReadAllBytesAsync(filePath);

            return (fileContent, mimeType, fileName);
        }

        // --------------------------------------------------------------------
        // FUNÇÕES DE OBTENÇÃO E FILTRAGEM DE DADOS
        // --------------------------------------------------------------------
        private async Task<object> GetFilteredDataAsync(ReportRequestDTO request)
        {
            // O DTO ReportFiltersDTO é o DTO correto para filtros de data
            var filtersToUse = request.Filters ?? new ReportFiltersDTO();

            switch (request.Type)
            {
                case ReportTypesEnum.MovementHeaders:
                    return await GetMovementHeadersAsync(filtersToUse);
                case ReportTypesEnum.MovementDetails:
                    if (!request.TargetMovementId.HasValue)
                        throw new ArgumentException("TargetMovementId é obrigatório para detalhes de movimento.");
                    return await GetMovementDetailsAsync(request.TargetMovementId.Value);
                case ReportTypesEnum.DeliveryHeaders:
                    return await GetDeliveryHeadersAsync(filtersToUse);
                default:
                    throw new ArgumentException($"Tipo de relatório '{request.Type}' não suportado.");
            }
        }

        private async Task<List<VMovHistory>> GetMovementHeadersAsync(ReportFiltersDTO filters)
        {
            var query = _dbContext.VMovHistories.AsQueryable();

            if (filters.DateFrom.HasValue)
            {
                // CORREÇÃO: Conversão de DateOnly para o início do dia (00:00:00)
                var dateTimeFrom = filters.DateFrom.Value.ToDateTime(TimeOnly.MinValue);
                query = query.Where(h => h.MovementDate >= dateTimeFrom);
            }

            if (filters.DateTo.HasValue)
            {
                // CORREÇÃO: Conversão de DateOnly para o FIM do dia (23:59:59.999...)
                var dateTimeTo = filters.DateTo.Value.ToDateTime(TimeOnly.MaxValue);
                query = query.Where(h => h.MovementDate <= dateTimeTo);
            }

            return await query.OrderByDescending(h => h.MovementDate).ToListAsync();
        }

        private async Task<List<VMovHistoryDetail>> GetMovementDetailsAsync(int movementId)
        {
            return await _dbContext.VMovHistoryDetails
                .Where(d => d.MovementId == movementId)
                .OrderBy(d => d.ProductBarcode)
                .ToListAsync();
        }

        private async Task<List<VDelivery>> GetDeliveryHeadersAsync(ReportFiltersDTO filters)
        {
            var query = _dbContext.Set<VDelivery>().AsQueryable();

            if (filters.DeliveryStatus.HasValue)
                query = query.Where(d => d.StatusId == filters.DeliveryStatus);

            if (filters.BeneficiaryId.HasValue)
                query = query.Where(d => d.BeneficiaryId == filters.BeneficiaryId.Value);

            if (filters.DateFrom.HasValue)
            {
                query = query.Where(d => d.ScheduledDate >= filters.DateFrom.Value);
            }

            if (filters.DateTo.HasValue)
            {
                query = query.Where(d => d.ScheduledDate <= filters.DateTo.Value);
            }

            return await query.OrderByDescending(d => d.ScheduledDate).ToListAsync();
        }

        // --------------------------------------------------------------------
        // GERAÇÃO DE CSV (Mantida)
        // --------------------------------------------------------------------
        private byte[] GenerateCsvContent(object data, ReportTypesEnum type)
        {
            var sb = new StringBuilder();

            if (type == ReportTypesEnum.MovementHeaders && data is List<VMovHistory> history)
            {
                sb.AppendLine("ID Movimento;Data;Tipo;Utilizador;Nota;Quantidade Total Afetada");
                foreach (var h in history)
                    sb.AppendLine($"{h.MovementId};{h.MovementDate:yyyy-MM-dd HH:mm};{h.MovementTypeId};{h.UserName};{h.MovementNote};{h.TotalQuantityAffected}");
            }
            else if (type == ReportTypesEnum.DeliveryHeaders && data is List<VDelivery> deliveries)
            {
                sb.AppendLine("ID Entrega;Data Agendada;Status;Beneficiário;Utilizador;Nota");
                foreach (var d in deliveries)
                    sb.AppendLine($"{d.DeliveryId};{d.ScheduledDate:yyyy-MM-dd};{d.StatusId};{d.BeneficiaryName};{d.UserName};{d.Note}");
            }
            else if (type == ReportTypesEnum.MovementDetails && data is List<VMovHistoryDetail> details)
            {
                sb.AppendLine("ID Movimento;Tipo;Data;Barcode;Produto;Data de Validade;Quantidade;Utilizador");
                foreach (var d in details)
                    sb.AppendLine($"{d.MovementId};{d.MovementTypeId};{d.MovementDate:yyyy-MM-dd HH:mm};{d.ProductBarcode};{d.ProductName};{d.GroupExpiryDate};{d.ItemQuantityAffected};{d.UserName}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        // --------------------------------------------------------------------
        // GERAÇÃO DE PDF (HTML para PDF) - Lógica de Conversão no topo
        // --------------------------------------------------------------------
        private byte[] GeneratePdfContent(object data, ReportTypesEnum type, ReportRequestDTO request, string reportTypeName)
        {
            // Chama o TemplateGeneratorService para obter o HTML preenchido
            // O uso de 'as dynamic' é necessário porque o tipo 'data' é genérico (object)
            var htmlContent = _templateGeneratorService.GenerateReportHtml((dynamic)data, type, request, reportTypeName);

            // Tipos do WkHtmlToPdfDotNet
            var globalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                DocumentTitle = request.FileName
            };

            var objectSettings = new ObjectSettings
            {
                HtmlContent = htmlContent,
                WebSettings =
                {
                    DefaultEncoding = "utf-8",
                    LoadImages = true
                },
                HeaderSettings = { FontSize = 10, Right = "Página [page] de [toPage]", Line = true },
            };

            var pdfDoc = new HtmlToPdfDocument
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings }
            };

            // O IConverter injetado no construtor realiza a conversão
            return _pdfConverter.Convert(pdfDoc);
        }
    }
}