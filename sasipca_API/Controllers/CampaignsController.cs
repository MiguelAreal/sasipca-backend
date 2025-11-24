using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sasipca_API.DBModels;
using sasipca_API.Dtos;
using sasipca_API.Models;
using sasipca_API.Services.Interfaces;

namespace sasipca_API.Controllers
{
    [Route("api/campaigns")]
    [ApiController]
    [Authorize]
    public class CampaignsController : ControllerBase
    {
        private readonly SasipcaContext _dbContext;
        private readonly IFileStorageService _fileStorageService;
        private const string ImageSubdirectory = "CampaignImages";

        public CampaignsController(SasipcaContext context, IFileStorageService fileStorageService)
        {
            _dbContext = context;
            _fileStorageService = fileStorageService;
        }

        // ----------------------------------------------------
        // ENDPOINT 1: CRIAÇÃO DE CAMPANHA (POST)
        // ----------------------------------------------------
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        public async Task<ActionResult> CreateCampaign([FromForm] CampaignPostDTO dto)
        {
            // 1. Validação
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (dto.StartDate >= dto.EndDate)
                return BadRequest(new Resposta("A data de início deve ser anterior à data de fim."));

            var userIdClaim = HttpContext.Items["UserId"];
            if (userIdClaim == null || !int.TryParse(userIdClaim.ToString(), out int userId))
                return Unauthorized(new Resposta("Utilizador não autenticado ou ID de utilizador inválido."));

            string? imageUrl = null;

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // 2. Guardar Imagem (se existir)
                if (dto.ImageFile != null)
                {
                    var uniqueFileName = $"{Guid.NewGuid()}-{dto.ImageFile.FileName}";
                    imageUrl = await _fileStorageService.SaveFileAsync(dto.ImageFile, ImageSubdirectory, uniqueFileName);
                }

                // 3. Criar a Campanha
                var newCampaign = new Campaign
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Location = dto.Location,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    UserId = userId,
                    ImageUrl = imageUrl
                };
                _dbContext.Campaigns.Add(newCampaign);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return StatusCode(StatusCodes.Status201Created, new Resposta("Campanha registada com sucesso."));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                if (imageUrl != null) _fileStorageService.DeleteFile(imageUrl, ImageSubdirectory);

                return StatusCode(StatusCodes.Status500InternalServerError, new Resposta("Erro ao criar a campanha."));
            }
        }

        // ----------------------------------------------------
        // ENDPOINT 2: ATUALIZAÇÃO DE CAMPANHA (PUT)
        // ----------------------------------------------------
        [HttpPut("{campaignId}")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        public async Task<ActionResult> UpdateCampaign(int campaignId, [FromForm] CampaignPutDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var campaign = await _dbContext.Campaigns.FindAsync(campaignId);
            if (campaign == null) return NotFound(new Resposta($"Campanha com ID {campaignId} não encontrada."));

            string? oldImageUrl = campaign.ImageUrl;
            string? newImageUrl = null;

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var newStartDate = dto.StartDate ?? campaign.StartDate;
                var newEndDate = dto.EndDate ?? campaign.EndDate;
                if (newStartDate >= newEndDate)
                    return BadRequest(new Resposta("A data de início deve ser anterior à data de fim."));

                // 1. Gestão da Imagem
                if (dto.RemoveImage && oldImageUrl != null)
                {
                    campaign.ImageUrl = null;
                    _fileStorageService.DeleteFile(oldImageUrl, ImageSubdirectory);
                    oldImageUrl = null;
                }
                else if (dto.NewImageFile != null)
                {
                    var uniqueFileName = $"{Guid.NewGuid()}-{dto.NewImageFile.FileName}";
                    newImageUrl = await _fileStorageService.SaveFileAsync(dto.NewImageFile, ImageSubdirectory, uniqueFileName);

                    campaign.ImageUrl = newImageUrl;

                    if (oldImageUrl != null)
                    {
                        _fileStorageService.DeleteFile(oldImageUrl, ImageSubdirectory);
                    }
                }

                // 2. Atualizar Campos de Texto
                campaign.Name = dto.Name;
                campaign.Description = dto.Description;
                campaign.Location = dto.Location;
                campaign.StartDate = newStartDate;
                campaign.EndDate = newEndDate;

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new Resposta("Campanha atualizada com sucesso."));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                if (newImageUrl != null) _fileStorageService.DeleteFile(newImageUrl, ImageSubdirectory);

                return StatusCode(StatusCodes.Status500InternalServerError, new Resposta("Erro ao atualizar a campanha."));
            }
        }

        // ----------------------------------------------------
        // ENDPOINT 3: CONSULTA DE CAMPANHAS (GET - Lista Paginada)
        // ----------------------------------------------------
        /// <summary>
        /// Lista campanhas com paginação, pesquisa e ordenação.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<CampaignHeaderDTO>))]
        public async Task<ActionResult<PaginatedResponse<CampaignHeaderDTO>>> GetCampaigns(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string orderBy = "desc", // Default: mais recentes primeiro
            [FromQuery] string searchTerm = "")
        {
            try
            {
                // 1. Validação de parâmetros
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 10;
                if (orderBy != "asc" && orderBy != "desc")
                    return BadRequest(new Resposta("Parâmetro orderBy deve ser 'asc' ou 'desc'"));

                var searchTermLower = searchTerm?.ToLower() ?? string.Empty;

                // 2. Query Base
                var query = _dbContext.Campaigns
                    .Include(c => c.User)
                    .AsQueryable();

                // 3. Pesquisa (Nome ou Descrição)
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(c => c.Name.ToLower().Contains(searchTermLower) ||
                                             (c.Description != null && c.Description.ToLower().Contains(searchTermLower)));
                }

                // 4. Projeção
                var projectedQuery = query.Select(c => new CampaignHeaderDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Location = c.Location,
                    ImageUrl = c.ImageUrl,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    CreatorName = c.User != null ? c.User.Name : "N/A"
                });

                // 5. Ordenação (Por Data de Início)
                projectedQuery = orderBy == "desc"
                    ? projectedQuery.OrderByDescending(c => c.StartDate)
                    : projectedQuery.OrderBy(c => c.StartDate);

                // 6. Paginação
                var totalCount = await projectedQuery.CountAsync();
                var pagedCampaigns = await projectedQuery
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                if (!pagedCampaigns.Any() && pageNumber == 1)
                    return NotFound(new Resposta("Nenhuma campanha encontrada."));

                // 7. Ajustar URLs das imagens
                foreach (var campaign in pagedCampaigns)
                {
                    campaign.ImageUrl = GetFullImageUrl(campaign.ImageUrl);
                }

                // 8. Construir Resposta
                var paginatedResponse = new PaginatedResponse<CampaignHeaderDTO>
                {
                    Data = pagedCampaigns,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                return Ok(paginatedResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new Resposta($"Ocorreu um erro interno ao obter as campanhas: {ex.Message}"));
            }
        }

        // ----------------------------------------------------
        // ENDPOINT 4: CONSULTA DE CAMPANHA ESPECÍFICA (GET {id} - Detalhe)
        // ----------------------------------------------------
        [HttpGet("{campaignId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CampaignHeaderDTO))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        public async Task<ActionResult<CampaignHeaderDTO>> GetCampaign(int campaignId)
        {
            var campaign = await _dbContext.Campaigns
                .Include(c => c.User)
                .Where(c => c.Id == campaignId)
                .Select(c => new CampaignHeaderDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Location = c.Location,
                    ImageUrl = c.ImageUrl,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    CreatorName = c.User != null ? c.User.Name : "N/A"
                })
                .FirstOrDefaultAsync();

            if (campaign == null)
                return NotFound(new Resposta($"Campanha com ID {campaignId} não encontrada."));

            // Aplica URL completo da imagem
            campaign.ImageUrl = GetFullImageUrl(campaign.ImageUrl);

            return Ok(campaign);
        }

        // ----------------------------------------------------
        // ENDPOINT 5: ELIMINAR CAMPANHA (DELETE)
        // ----------------------------------------------------
        /// <summary>
        /// Elimina uma campanha e a sua imagem associada.
        /// </summary>
        [HttpDelete("{campaignId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(Resposta))]
        public async Task<ActionResult<Resposta>> DeleteCampaign(int campaignId)
        {
            try
            {
                // 1. Buscar a campanha
                var campaign = await _dbContext.Campaigns.FindAsync(campaignId);

                if (campaign == null)
                    return NotFound(new Resposta($"Campanha com ID {campaignId} não encontrada."));

                // 2. Apagar a imagem física do servidor (se existir)
                // Isto garante que não fica com ficheiros inúteis a ocupar espaço
                if (!string.IsNullOrEmpty(campaign.ImageUrl))
                {
                    // O método DeleteFile do teu serviço deve tratar de não dar erro se o ficheiro já não existir
                    _fileStorageService.DeleteFile(campaign.ImageUrl, ImageSubdirectory);
                }

                // 3. Remover da Base de Dados
                _dbContext.Campaigns.Remove(campaign);
                await _dbContext.SaveChangesAsync();

                // 4. Retornar sucesso
                return Ok(new Resposta("Campanha eliminada com sucesso."));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new Resposta($"Erro ao eliminar a campanha: {ex.Message}"));
            }
        }


        #region Funções Auxiliares
        private string GetFullImageUrl(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return string.Empty;

            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";

            return $"{baseUrl}/static/{ImageSubdirectory}/{fileName}";
        }
        #endregion
    }
}