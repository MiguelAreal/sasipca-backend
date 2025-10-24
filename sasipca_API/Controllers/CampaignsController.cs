using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sasipca_API.DBModels;
using sasipca_API.Dtos;
using sasipca_API.Models;
using sasipca_API.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Route("api/campaigns")]
[ApiController]
[Authorize]
public class CampaignsController : ControllerBase
{
    private readonly SasipcaContext _dbContext;
    private readonly IFileStorageService _fileStorageService; // Serviço para guardar imagens
    private const string ImageSubdirectory = "CampaignImages"; // Pasta de guarda no servidor

    public CampaignsController(SasipcaContext context, IFileStorageService fileStorageService)
    {
        _dbContext = context;
        _fileStorageService = fileStorageService;
    }

    // ----------------------------------------------------
    // ENDPOINT 1: CRIAÇÃO DE CAMPANHA (POST)
    // ----------------------------------------------------
    /// <summary>
    /// Regista uma nova Campanha com a opção de upload de uma imagem.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")] // Necessário para receber IFormFile
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
                // A função SaveFileAsync deve retornar o nome único do ficheiro
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
            // Tentar apagar o ficheiro se a transação da BD falhar
            if (imageUrl != null) _fileStorageService.DeleteFile(imageUrl, ImageSubdirectory);

            return StatusCode(StatusCodes.Status500InternalServerError, new Resposta("Erro ao criar a campanha."));
        }
    }

    // ----------------------------------------------------
    // ENDPOINT 2: ATUALIZAÇÃO DE CAMPANHA (PUT)
    // ----------------------------------------------------
    /// <summary>
    /// Atualiza os dados de uma Campanha, incluindo substituição ou remoção da imagem.
    /// </summary>
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
            // Validação de datas
            var newStartDate = dto.StartDate ?? campaign.StartDate;
            var newEndDate = dto.EndDate ?? campaign.EndDate;
            if (newStartDate >= newEndDate)
                return BadRequest(new Resposta("A data de início deve ser anterior à data de fim."));

            // 1. Gestão da Imagem
            if (dto.RemoveImage && oldImageUrl != null)
            {
                // A. Remoção de imagem existente
                campaign.ImageUrl = null;
                _fileStorageService.DeleteFile(oldImageUrl, ImageSubdirectory);
                oldImageUrl = null; // Para evitar deleção dupla em caso de exceção
            }
            else if (dto.NewImageFile != null)
            {
                // B. Substituição/Adição de nova imagem
                var uniqueFileName = $"{Guid.NewGuid()}-{dto.NewImageFile.FileName}";
                newImageUrl = await _fileStorageService.SaveFileAsync(dto.NewImageFile, ImageSubdirectory, uniqueFileName);

                campaign.ImageUrl = newImageUrl;

                // Remover o ficheiro antigo APÓS o novo ter sido guardado com sucesso
                if (oldImageUrl != null)
                {
                    _fileStorageService.DeleteFile(oldImageUrl, ImageSubdirectory);
                }
            }
            // Se NewImageFile for null e RemoveImage for false, ImageUrl mantém-se inalterado.

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
            // Tentar limpar o NOVO ficheiro (se foi guardado mas a transação falhou)
            if (newImageUrl != null) _fileStorageService.DeleteFile(newImageUrl, ImageSubdirectory);

            // NOTA: Se o ficheiro antigo foi removido no passo B e a transação falhou, ele está PERDIDO.
            // Para ser 100% seguro, o DELETE deveria ser assíncrono APÓS o Commit.

            return StatusCode(StatusCodes.Status500InternalServerError, new Resposta("Erro ao atualizar a campanha."));
        }
    }

    // ----------------------------------------------------
    // ENDPOINT 3: CONSULTA DE CAMPANHAS (GET - Lista)
    // ----------------------------------------------------
    /// <summary>
    /// Lista todas as Campanhas com os dados resumidos.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CampaignHeaderDTO>))]
    public async Task<ActionResult<IEnumerable<CampaignHeaderDTO>>> GetAllCampaigns()
    {
        var campaigns = await _dbContext.Campaigns
            .Include(c => c.User) // Incluir o utilizador criador
            .OrderByDescending(c => c.StartDate)
            .Select(c => new CampaignHeaderDTO
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Location = c.Location,
                ImageUrl = c.ImageUrl,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                CreatorName = c.User != null ? c.User.Name : "N/A" // Mapeia o nome do utilizador
            })
            .ToListAsync();

        foreach (var campaign in campaigns)
        {
            // Altera a propriedade ImageUrl no objeto DTO.
            campaign.ImageUrl = GetFullImageUrl(campaign.ImageUrl);
        }

        return Ok(campaigns);
    }

    // ----------------------------------------------------
    // ENDPOINT 4: CONSULTA DE CAMPANHA ESPECÍFICA (GET {id} - Detalhe)
    // ----------------------------------------------------
    /// <summary>
    /// Busca uma Campanha específica pelo ID com detalhes completos.
    /// </summary>
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
        
        //Aplica URL completo da imagem
        campaign.ImageUrl = GetFullImageUrl(campaign.ImageUrl);

        return Ok(campaign);
    }


    private string GetFullImageUrl(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return string.Empty;

        // Constrói a URL base (esquema + host + porta, se aplicável)
        var request = HttpContext.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";

        // Constrói a URL completa usando o prefixo /static e o subdiretório
        return $"{baseUrl}/static/{ImageSubdirectory}/{fileName}";
    }



}