using sasipca_API.DBModels;
using sasipca_API.Dtos;
using sasipca_API.Enumerators;
using static sasipca_API.Enumerators.Enums;

namespace sasipca_API.Services.Interfaces
{
    public interface ITemplateGeneratorService
    {
        /// <summary>
        /// Gera o conteúdo HTML do relatório com base nos dados.
        /// </summary>
        /// <typeparam name="T">O tipo de dados do relatório.</typeparam>
        /// <param name="data">Os dados brutos do relatório.</param>
        /// <param name="type">O tipo de relatório.</param>
        /// <returns>O conteúdo HTML como string.</returns>
        string GenerateReportHtml<T>(T data, ReportTypesEnum type, ReportRequestDTO request, string reportTypeName);
    }
}