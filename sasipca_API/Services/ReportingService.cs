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
    // SERVIÇO PRINCIPAL DE RELATÓRIOS (HTML-to-PDF e CSV)
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
        // MÉTODO PRINCIPAL DA INTERFACE
        // --------------------------------------------------------------------
        public async Task<(byte[] fileContent, string mimeType, string fileName, int newReportId)> GenerateReportAsync(ReportRequestDTO request, int creatorId)
        {
            // 1. Obter Nome do Tipo de Relatório para o título
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

            // 2. [NOVO] Carregar cache de Tipos de Movimento (ID -> Nome)
            // Isto evita ir à base de dados linha a linha durante a geração do CSV
            var movementTypesMap = await _dbContext.MovementTypes
                .ToDictionaryAsync(k => k.Id, v => v.Type);

            var fileExtension = request.Format == ReportFormat.PDF ? "pdf" : "csv";
            var baseFileName = request.FileName.Replace(" ", "_");
            var finalFileName = $"{baseFileName}.{fileExtension}";

            var data = await GetFilteredDataAsync(request);

            if (data == null || (data is IEnumerable<object> enumerable && !enumerable.Any()))
            {
                return (Array.Empty<byte>(), "application/octet-stream", finalFileName, 0);
            }

            byte[] fileContent;
            string mimeType;

            if (request.Format == ReportFormat.CSV)
            {
                // Passamos o mapa de tipos para o gerador de CSV
                fileContent = GenerateCsvContent(data, (ReportTypesEnum)request.Type, movementTypesMap);
                mimeType = "text/csv";
            }
            else // PDF
            {
                // O TemplateGeneratorService (se atualizado conforme passos anteriores) trata disto,
                // mas a lógica principal de dados é passada aqui.
                fileContent = GeneratePdfContent(data, (ReportTypesEnum)request.Type, request, reportTypeName);
                mimeType = "application/pdf";
            }

            // 3. Guardar no Disco
            var reportsDirectory = Path.Combine(Directory.GetCurrentDirectory(),"Storage", "Reports");
            if (!Directory.Exists(reportsDirectory))
            {
                Directory.CreateDirectory(reportsDirectory);
            }
            var filePath = Path.Combine(reportsDirectory, finalFileName);
            await File.WriteAllBytesAsync(filePath, fileContent);

            // 4. Guardar na Base de Dados
            var reportEntry = new Report
            {
                Name = finalFileName,
                CreatorId = creatorId,
                ReportType = (int)request.Type
            };

            _dbContext.Reports.Add(reportEntry);
            await _dbContext.SaveChangesAsync();

            return (fileContent, mimeType, finalFileName, reportEntry.Id);
        }

        // --------------------------------------------------------------------
        // MÉTODOS AUXILIARES (Listagem e Download)
        // --------------------------------------------------------------------
        public async Task<IEnumerable<ReportGetDTO>> GetGeneratedReportsMetadataAsync(int? reportTypeId = null)
        {
            var query = _dbContext.Reports.AsQueryable();

            if (reportTypeId.HasValue && reportTypeId.Value > 0)
            {
                query = query.Where(r => r.ReportType == reportTypeId.Value);
            }

            return await query
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
        }

        public async Task<(byte[] fileContent, string mimeType, string fileName)> GetReportFileAsync(int reportId)
        {
            var reportEntry = await _dbContext.Reports.FindAsync(reportId);

            if (reportEntry == null)
            {
                throw new KeyNotFoundException($"Relatório com ID {reportId} não encontrado.");
            }

            var fileName = reportEntry.Name;
            var reportsDirectory = Path.Combine(Directory.GetCurrentDirectory(),"Storage", "Reports");
            var filePath = Path.Combine(reportsDirectory, fileName);

            var mimeType = fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? "application/pdf" : "text/csv";

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"O ficheiro '{fileName}' não foi encontrado no disco.");
            }

            byte[] fileContent = await File.ReadAllBytesAsync(filePath);
            return (fileContent, mimeType, fileName);
        }

        // --------------------------------------------------------------------
        // OBTENÇÃO DE DADOS
        // --------------------------------------------------------------------
        private async Task<object> GetFilteredDataAsync(ReportRequestDTO request)
        {
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
                query = query.Where(h => h.MovementDate >= filters.DateFrom.Value.ToDateTime(TimeOnly.MinValue));

            if (filters.DateTo.HasValue)
                query = query.Where(h => h.MovementDate <= filters.DateTo.Value.ToDateTime(TimeOnly.MaxValue));

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
                query = query.Where(d => d.ScheduledDate >= filters.DateFrom.Value);

            if (filters.DateTo.HasValue)
                query = query.Where(d => d.ScheduledDate <= filters.DateTo.Value);

            return await query.OrderByDescending(d => d.ScheduledDate).ToListAsync();
        }

        // --------------------------------------------------------------------
        // GERAÇÃO DE CSV (ATUALIZADA)
        // --------------------------------------------------------------------
        private byte[] GenerateCsvContent(object data, ReportTypesEnum type, Dictionary<int, string> movTypes)
        {
            var sb = new StringBuilder();

            // Função auxiliar local para obter o nome do tipo
            string GetMovTypeName(int typeId) => movTypes.ContainsKey(typeId) ? movTypes[typeId] : typeId.ToString();

            if (type == ReportTypesEnum.MovementHeaders && data is List<VMovHistory> history)
            {
                sb.AppendLine("ID Movimento;Data;Tipo;Utilizador;Nota;Quantidade Total Afetada");
                foreach (var h in history)
                {
                    // [CORREÇÃO]: Usar GetMovTypeName em vez de h.MovementTypeId
                    sb.AppendLine($"{h.MovementId};{h.MovementDate:yyyy-MM-dd HH:mm};{GetMovTypeName(h.MovementTypeId)};{h.UserName};{h.MovementNote};{h.TotalQuantityAffected}");
                }
            }
            else if (type == ReportTypesEnum.DeliveryHeaders && data is List<VDelivery> deliveries)
            {
                // Nota: Poderia fazer o mesmo para StatusId se tivesses um dicionário de Status
                sb.AppendLine("ID Entrega;Data Agendada;Status;Beneficiário;Utilizador;Nota");
                foreach (var d in deliveries)
                {
                    sb.AppendLine($"{d.DeliveryId};{d.ScheduledDate:yyyy-MM-dd};{d.StatusId};{d.BeneficiaryName};{d.UserName};{d.Note}");
                }
            }
            else if (type == ReportTypesEnum.MovementDetails && data is List<VMovHistoryDetail> details)
            {
                sb.AppendLine("ID Movimento;Tipo;Data;Barcode;Produto;Data de Validade;Quantidade;Utilizador");
                foreach (var d in details)
                {
                    // [CORREÇÃO]: Usar GetMovTypeName em vez de d.MovementTypeId
                    sb.AppendLine($"{d.MovementId};{GetMovTypeName(d.MovementTypeId)};{d.MovementDate:yyyy-MM-dd HH:mm};{d.ProductBarcode};{d.ProductName};{d.GroupExpiryDate};{d.ItemQuantityAffected};{d.UserName}");
                }
            }

            // Adiciona BOM (Byte Order Mark) para o Excel abrir o UTF-8 corretamente
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        // --------------------------------------------------------------------
        // GERAÇÃO DE PDF
        // --------------------------------------------------------------------
        private byte[] GeneratePdfContent(object data, ReportTypesEnum type, ReportRequestDTO request, string reportTypeName)
        {
            // O serviço de template já trata da conversão ID -> Nome internamente (conforme configurado anteriormente)
            var htmlContent = _templateGeneratorService.GenerateReportHtml((dynamic)data, type, request, reportTypeName);

            var globalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                DocumentTitle = request.FileName,
                Margins = new MarginSettings { Top = 10, Bottom = 10 }
            };

            var objectSettings = new ObjectSettings
            {
                HtmlContent = htmlContent,
                WebSettings = { DefaultEncoding = "utf-8", LoadImages = true },
                HeaderSettings = { FontSize = 10, Right = "Página [page] de [toPage]", Line = true },
            };

            var pdfDoc = new HtmlToPdfDocument
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings }
            };

            return _pdfConverter.Convert(pdfDoc);
        }
    }
}