using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sasipca_API.Attributes;
using sasipca_API.DBModels;
using sasipca_API.Dtos;
using sasipca_API.Enumerators;
using sasipca_API.Models;
using sasipca_API.Services;
using sasipca_API.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static sasipca_API.Enumerators.Enums;

namespace sasipca_API.Controllers
{
    /// <summary>
    /// Controller para gestão de beneficiários.
    /// </summary>
    [Route("api/beneficiaries")]
    [ApiController]
    public class BeneficiariesController : ControllerBase
    {
        private readonly SasipcaContext _dbContext;
        private readonly IBeneficiaryService _beneficiaryService;

        public BeneficiariesController(SasipcaContext context, IBeneficiaryService beneficiaryService)
        {
            _dbContext = context;
            _beneficiaryService = beneficiaryService;
        }

        // ----------------------------------------------------
        // CRIAR PERFIL (POST) - APENAS ADMIN
        // ----------------------------------------------------
        /// <summary>
        /// Registo de novo perfil de beneficiário.
        /// </summary>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(Resposta))]
        [Produces("application/json")]
        [HttpPost()]
        [AuthorizeRole(UserRole.Admin)] // <--- BLOQUEIO AQUI
        public async Task<IActionResult> PostBeneficiary([FromBody] BeneficiaryPostDTO beneficiaryPostDto)
        {
            try
            {
                // Busca id do user admin a efetuar o registo.
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                if (await _dbContext.Users.AnyAsync(p => p.Email == beneficiaryPostDto.Email))
                    return BadRequest(new Resposta("Este e-mail já está registado."));

                if (await _dbContext.Users.AnyAsync(p => p.Contact == beneficiaryPostDto.Contact))
                    return BadRequest(new Resposta("Este contacto já está registado."));

                var beneficiary = new Beneficiary
                {
                    Name = beneficiaryPostDto.Name,
                    Email = beneficiaryPostDto.Email,
                    Contact = beneficiaryPostDto.Contact,
                    Course = beneficiaryPostDto.Course,
                    CurricularYear = beneficiaryPostDto.CurricularYear,
                    StudentNum = beneficiaryPostDto.StudentNum,
                    Nif = beneficiaryPostDto.Nif,
                    GlobalObs = beneficiaryPostDto.GlobalObs,
                    CreatedBy = userId,
                    Address = new BeneficiaryAddress
                    {
                        Street = beneficiaryPostDto.Street,
                        Number = beneficiaryPostDto.Number ?? 0,
                        PostalCode = beneficiaryPostDto.PostalCode
                    }
                };

                await _dbContext.Beneficiaries.AddAsync(beneficiary);
                await _dbContext.SaveChangesAsync();

                // Cria observação particular (se existir no DTO)
                if (!string.IsNullOrWhiteSpace(beneficiaryPostDto.ParticularObs))
                {
                    var particularOb = new ParticularOb
                    {
                        UserId = userId,
                        BeneficiaryId = beneficiary.Id,
                        Obs = beneficiaryPostDto.ParticularObs
                    };

                    await _dbContext.ParticularObs.AddAsync(particularOb);
                    await _dbContext.SaveChangesAsync();
                }

                return Ok(new Resposta("Perfil criado com sucesso."));
            }
            catch (Exception ex)
            {
                return BadRequest(new Resposta("Erro ao criar perfil: " + ex.Message));
            }
        }

        // ----------------------------------------------------
        // EDITAR PERFIL (PUT) - ADMIN (TODOS) OU BENEFICIÁRIO (PRÓPRIO)
        // ----------------------------------------------------
        /// <summary>
        /// Atualizar perfil de beneficiário.
        /// </summary>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [Produces("application/json")]
        [HttpPut("{beneficiaryId}")]
        [AuthorizeRole(UserRole.Admin, UserRole.Beneficiary)] // <--- AMBOS PODEM ACEDER
        public async Task<IActionResult> PutBeneficiary(int beneficiaryId, [FromBody] BeneficiaryPostDTO beneficiaryPutDto)
        {
            try
            {
                // 1. Extrair ID e Role do Token
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var roleStr = User.FindFirstValue(ClaimTypes.Role);

                if (userIdStr == null || roleStr == null || !int.TryParse(userIdStr, out int userId) || !Enum.TryParse(roleStr, out UserRole userRole))
                {
                    return Unauthorized(new Resposta("Utilizador não autenticado."));
                }

                // 2. SEGURANÇA: Se for Beneficiário e o ID URL != ID Token -> PROIBIDO
                if (userRole == UserRole.Beneficiary && userId != beneficiaryId)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new Resposta("Não tem permissão para editar este perfil."));
                }

                // Buscar beneficiário existente
                var beneficiary = await _dbContext.Beneficiaries
                    .Include(b => b.Address)
                    .Include(b => b.ParticularObs)
                    .FirstOrDefaultAsync(b => b.Id == beneficiaryId);

                if (beneficiary == null)
                    return NotFound(new Resposta("Beneficiário não encontrado."));

                // Verificar duplicações (ignorando o próprio)
                if (await _dbContext.Beneficiaries.AnyAsync(p => p.Email == beneficiaryPutDto.Email && p.Id != beneficiaryId))
                    return BadRequest(new Resposta("Este e-mail já está registado noutro perfil."));

                if (await _dbContext.Beneficiaries.AnyAsync(p => p.Contact == beneficiaryPutDto.Contact && p.Id != beneficiaryId))
                    return BadRequest(new Resposta("Este contacto já está registado noutro perfil."));

                // Atualizar os campos principais
                beneficiary.Name = beneficiaryPutDto.Name;
                beneficiary.Email = beneficiaryPutDto.Email;
                beneficiary.Contact = beneficiaryPutDto.Contact;
                beneficiary.Course = beneficiaryPutDto.Course;
                beneficiary.CurricularYear = beneficiaryPutDto.CurricularYear;
                beneficiary.GlobalObs = beneficiaryPutDto.GlobalObs;
                beneficiary.Nif = beneficiaryPutDto.Nif;
                beneficiary.StudentNum = beneficiaryPutDto.StudentNum;

                // Atualizar ou criar morada
                if (!string.IsNullOrWhiteSpace(beneficiaryPutDto.Street) || beneficiaryPutDto.Number.HasValue || !string.IsNullOrWhiteSpace(beneficiaryPutDto.PostalCode))
                {
                    if (beneficiary.Address == null)
                    {
                        beneficiary.Address = new BeneficiaryAddress();
                    }

                    beneficiary.Address.Street = beneficiaryPutDto.Street ?? "";
                    beneficiary.Address.Number = beneficiaryPutDto.Number ?? 0;
                    beneficiary.Address.PostalCode = beneficiaryPutDto.PostalCode ?? "";
                }
                else
                {
                    beneficiary.Address = null; // Se limpar tudo, remove a morada
                }

                // Upsert da observação particular
                // NOTA: Se for o próprio aluno a editar, 'userId' é o ID dele.
                // Ele pode criar uma "nota pessoal" sobre o seu perfil, mas não edita a nota do Admin.
                if (!string.IsNullOrWhiteSpace(beneficiaryPutDto.ParticularObs))
                {
                    var obs = beneficiary.ParticularObs.FirstOrDefault(o => o.UserId == userId);

                    if (obs != null)
                    {
                        obs.Obs = beneficiaryPutDto.ParticularObs;
                    }
                    else
                    {
                        beneficiary.ParticularObs.Add(new ParticularOb
                        {
                            UserId = userId, // Admin ID ou Beneficiary ID
                            BeneficiaryId = beneficiaryId,
                            Obs = beneficiaryPutDto.ParticularObs
                        });
                    }
                }

                await _dbContext.SaveChangesAsync();

                return Ok(new Resposta("Perfil atualizado com sucesso."));
            }
            catch (Exception ex)
            {
                return BadRequest(new Resposta($"Erro ao atualizar perfil: {ex.Message}"));
            }
        }

        // ----------------------------------------------------
        // LISTAR TODOS (GET) - APENAS ADMIN
        // ----------------------------------------------------
        /// <summary>
        /// Busca todos os beneficiários existentes consoante filtros.
        /// </summary>
        [HttpGet()]
        [AuthorizeRole(UserRole.Admin)] // <--- BLOQUEIO AQUI
        public async Task<ActionResult<PaginatedResponse<BeneficiaryListDTO>>> GetProfiles(
             [FromQuery] int pageNumber = 1,
             [FromQuery] int pageSize = 10,
             [FromQuery] string orderBy = "asc",
             [FromQuery] string searchTerm = "")
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 10;
                if (orderBy != "asc" && orderBy != "desc")
                    return BadRequest(new Resposta("Parâmetro orderBy deve ser 'asc' ou 'desc'"));

                var beneficiaries = await _beneficiaryService.GetBeneficiaries(searchTerm);

                beneficiaries = orderBy == "desc"
                    ? beneficiaries.OrderByDescending(a => a.Name).ToList()
                    : beneficiaries.OrderBy(a => a.Name).ToList();

                if (!beneficiaries.Any())
                {
                    return NotFound(new Resposta("Nenhum beneficiário encontrado."));
                }

                var totalCount = beneficiaries.Count;
                var pagedBeneficiaries = beneficiaries
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var paginatedResponse = new PaginatedResponse<BeneficiaryListDTO>
                {
                    Data = pagedBeneficiaries,
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
                                  new Resposta("Ocorreu um erro interno ao obter os beneficiários."));
            }
        }

        // ----------------------------------------------------
        // OBTER PERFIL ÚNICO (GET) - JÁ CONFIGURADO ANTES
        // ----------------------------------------------------
        [HttpGet("{beneficiaryId}")]
        [AuthorizeRole(UserRole.Admin, UserRole.Beneficiary)]
        public async Task<ActionResult> GetProfile(int beneficiaryId)
        {
            // ... (Manter o código da resposta anterior que valida se userId == beneficiaryId) ...
            // Vou replicar aqui para ficar completo o ficheiro:

            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var roleStr = User.FindFirstValue(ClaimTypes.Role);

                if (userIdStr == null || roleStr == null || !int.TryParse(userIdStr, out int userId) || !Enum.TryParse(roleStr, out UserRole userRole))
                {
                    return Unauthorized(new Resposta("Utilizador não autenticado."));
                }

                // SEGURANÇA: Beneficiário só vê o seu
                if (userRole == UserRole.Beneficiary && userId != beneficiaryId)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new Resposta("Não tem permissão para visualizar este perfil."));
                }

                var beneficiary = await _dbContext.Beneficiaries
                    .Where(b => b.Id == beneficiaryId)
                    .Include(b => b.Address)
                    .Include(b => b.ParticularObs)
                    .Select(b => new BeneficiaryGetDTO
                    {
                        BeneficiaryId = b.Id,
                        Name = b.Name,
                        Email = b.Email,
                        Contact = b.Contact,
                        Course = b.Course,
                        CurricularYear = b.CurricularYear,
                        StudentNum = b.StudentNum,
                        Nif = b.Nif,
                        GlobalObs = b.GlobalObs,
                        // Filtra para ver apenas a observação criada por QUEM está a ver (Admin ou o próprio)
                        ParticularObs = b.ParticularObs
                            .Where(po => po.UserId == userId)
                            .Select(po => po.Obs)
                            .FirstOrDefault(),
                        Street = b.Address != null ? b.Address.Street : null,
                        Number = b.Address != null ? b.Address.Number : null,
                        PostalCode = b.Address != null ? b.Address.PostalCode : null
                    })
                    .FirstOrDefaultAsync();

                if (beneficiary == null)
                    return NotFound(new Resposta("Perfil não encontrado."));

                return Ok(beneficiary);
            }
            catch (Exception ex)
            {
                return BadRequest(new Resposta($"Ocorreu um erro ao obter o perfil: {ex.Message}"));
            }
        }
    }
}