using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sasipca_API.Attributes;
using sasipca_API.Dtos;
using sasipca_API.Models;
using sasipca_API.Services.Interfaces;
using System;
using System.IO; // Necessário para Path
using System.Security.Claims; // Necessário para claims
using System.Threading.Tasks;
using static sasipca_API.Dtos.ReportRequestDTO;
using static sasipca_API.Enumerators.Enums;

namespace sasipca_API.Controllers
{

    // <summary>
    /// Controller para geração de relatórios dinâmicos de inventário, movimentos e entregas.
    /// Requer autenticação por Bearer Token.
    /// </summary>
    [Route("api/reports")]
    [ApiController]
    [AuthorizeRole(UserRole.Admin)]
    public class ReportsController : ControllerBase
    {
        private readonly IReportingService _reportingService;

        public ReportsController(IReportingService reportingService)
        {
            _reportingService = reportingService;
        }


        /// <summary>
        /// Gera um relatório dinâmico em formato PDF (1) ou CSV (2) com base no tipo de dados e filtros fornecidos. 
        /// </summary>
        /// <remarks>
        /// **Tipos de Relatório (type):**
        /// * `1` (MovementHeaders): Sumário de todos os movimentos de stock. (Usa filtros de data)
        /// * `2` (MovementDetails): Detalhe de itens de um movimento específico. (Requer targetMovementId)
        /// * `3` (DeliveryHeaders): Sumário de todas as entregas. (Usa filtros de data, status, beneficiário opcionais)
        /// 
        /// **Formatos (format):**
        /// 
        /// * `1` (PDF): Retorna "application/pdf".
        /// * `2` (CSV): Retorna "text/csv".
        /// </remarks> 
        /// <param name="request">Detalhes do relatório, formato e filtros.</param> 
        /// <returns>Um ficheiro (FileStreamResult) contendo o relatório.</returns> 
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileStreamResult))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(Resposta))]
        public async Task<ActionResult> GenerateReport([FromBody] ReportRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            //Busca id do user a efetuar o registo.
            int userId = (int)HttpContext.Items["UserId"];

            // Validação de regras específicas do relatório
            if (request.Type == ReportTypesEnum.MovementDetails && !request.TargetMovementId.HasValue)
            {
                return BadRequest(new Resposta("Para o tipo de relatório 'MovementDetails', é obrigatório fornecer o TargetMovementId."));
            }

            var safeFileName = Path.GetFileNameWithoutExtension(request.FileName);
            if (string.IsNullOrWhiteSpace(safeFileName))
            {
                return BadRequest(new Resposta("O nome do ficheiro fornecido é inválido."));
            }

            try
            {
                // 1. Chamar o serviço de relatórios (passando o userId)
                var (fileContent, mimeType, finalFileName, newReportId) = await _reportingService.GenerateReportAsync(request, userId);

                if (fileContent == null || fileContent.Length == 0)
                {
                    return NotFound(new Resposta($"Não foram encontrados dados para gerar o relatório '{request.Type}'."));
                }

                // 2. Retornar o arquivo como File Content
                // NOTA: O 'newReportId' é retornado no serviço, mas usado aqui apenas
                // para confirmação interna ou futura auditoria.
                return File(fileContent, mimeType, finalFileName);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new Resposta(ex.Message));
            }
            catch (Exception ex)
            {
                // Aqui você pode logar o ex.Message e o StackTrace para diagnosticar
                return StatusCode(StatusCodes.Status500InternalServerError, new Resposta("Ocorreu um erro interno ao gerar o relatório: " + ex.Message));
            }
        }

        /// <summary>
        /// Lista os metadados dos relatórios gerados no sistema.
        /// </summary>
        /// <param name="reportType">Opcional: ID do tipo de relatório para filtrar (1=Movimentos, 2=Detalhe de Movimento, 3=Entregas).</param>
        /// <returns>Lista de metadados do relatório (ID, Nome, Criador, Tipo, Data).</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ReportGetDTO>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(Resposta))]
        public async Task<ActionResult<IEnumerable<ReportGetDTO>>> GetGeneratedReports([FromQuery] ReportTypesEnum? reportType)
        {
            try
            {
                // 1. Opcional: Converter o Enum de volta para int? para o serviço
                // O cast direto para int funciona perfeitamente para Enums anuláveis.
                int? reportTypeId = (int?)reportType;

                // 2. Chamar o serviço
                var reportsMetadata = await _reportingService.GetGeneratedReportsMetadataAsync(reportTypeId);

                if (!reportsMetadata.Any())
                {
                    return Ok(new List<ReportGetDTO>());
                }

                return Ok(reportsMetadata);
            }
            catch (Exception ex)
            {
                // Log do erro
                return StatusCode(StatusCodes.Status500InternalServerError, new Resposta("Ocorreu um erro interno ao listar os relatórios: " + ex.Message));
            }
        }

        /// <summary>
        /// Obtém o ficheiro de relatório (PDF ou CSV) previamente gerado e guardado, usando seu ID de registo.
        /// </summary>
        /// <param name="id">ID do registo do relatório na base de dados.</param>
        /// <returns>O ficheiro binário (PDF ou CSV).</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileContentResult))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        public async Task<ActionResult> GetReportFile(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new Resposta("ID de relatório inválido."));
            }

            try
            {
                var (fileContent, mimeType, fileName) = await _reportingService.GetReportFileAsync(id);

                return File(fileContent, mimeType, fileName);
            }
            catch (KeyNotFoundException ex)
            {
                // Captura erro do DB (ID não encontrado)
                return NotFound(new Resposta(ex.Message));
            }
            catch (FileNotFoundException ex)
            {
                // Captura erro do disco (Ficheiro não existe)
                return NotFound(new Resposta(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Resposta("Erro interno ao obter o ficheiro: " + ex.Message));
            }
        }


    }
}