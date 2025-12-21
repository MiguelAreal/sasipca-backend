using sasipca_API.Dtos;
using sasipca_API.Models;
using System.Threading.Tasks;

namespace sasipca_API.Services.Interfaces
{
    public interface IReportingService
    {
        /// <summary>
        /// Gera o relatório com base nos parâmetros e retorna o conteúdo do ficheiro.
        /// </summary>
        /// <param name="request">A solicitação de relatório.</param>
        /// <returns>O conteúdo binário do ficheiro e o tipo de mídia (MimeType).</returns>
        Task<(byte[] fileContent, string mimeType, string fileName, int newReportId)> GenerateReportAsync(ReportRequestDTO request, int creatorId);

        /// <summary>
        /// Obtém a lista de relatórios gerados com metadados detalhados, opcionalmente filtrada por tipo.
        /// </summary>
        /// <param name="reportTypeId">Opcional: ID do tipo de relatório para filtrar (1, 2 ou 3).</param>
        Task<IEnumerable<ReportGetDTO>> GetGeneratedReportsMetadataAsync(int? reportTypeId = null);

        /// <summary>
        /// Busca o conteúdo do ficheiro de relatório guardado no disco.
        /// </summary>
        /// <param name="reportId">O ID do registo do relatório na base de dados.</param>
        /// <returns>O conteúdo binário do ficheiro, tipo MIME e nome do ficheiro.</returns>
        Task<(byte[] fileContent, string mimeType, string fileName)> GetReportFileAsync(int reportId);
    }
}