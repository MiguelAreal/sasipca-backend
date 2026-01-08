using sasipca_API.DBModels;
using sasipca_API.Dtos;
using static sasipca_API.Enumerators.Enums;

namespace sasipca_API.Services.Interfaces
{
    public interface ITemplateGeneratorService
    {
        /// <summary>
        /// Gera o documento PDF do relatório com base nos dados usando QuestPDF.
        /// </summary>
        /// <typeparam name="T">O tipo de dados do relatório.</typeparam>
        /// <param name="data">Os dados brutos do relatório.</param>
        /// <param name="type">O tipo de relatório.</param>
        /// <param name="request">Objeto de requisição com filtros.</param>
        /// <param name="reportTypeName">Nome do tipo de relatório.</param>
        /// <returns>O documento PDF como byte array.</returns>
        byte[] GenerateReportPdf<T>(T data, ReportTypesEnum type, ReportRequestDTO request, string reportTypeName);
    }
}