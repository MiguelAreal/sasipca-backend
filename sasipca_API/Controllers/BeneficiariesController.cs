using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sasipca_API.Models;
using sasipca_API.Services;
using sasipca_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using sasipca_API.Dtos;
using sasipca_API.DBModels;
using Humanizer;

namespace sasipca_API.Controllers
{
    /// <summary>
    /// Controller para gestão de beneficiários.
    /// </summary>
    [Route("api/beneficiaries")]
    [ApiController]
    [Authorize]
    public class BeneficiariesController : ControllerBase
    {
        private readonly SasipcaContext _dbContext;
        private readonly IBeneficiaryService _beneficiaryService;

        /// <summary>
        /// Inicialização do BeneficiariesController.
        /// </summary>
        /// <param name="beneficiaryService">Serviço de beneficiários</param>
        /// <param name="context">Contexto da base de dados</param>
        public BeneficiariesController(SasipcaContext context, IBeneficiaryService beneficiaryService)
        {
            _dbContext = context;
            _beneficiaryService = beneficiaryService;
        }

        /// <summary>
        /// Registo de novo perfil de beneficiário.
        /// </summary>
        /// <remarks>
        /// Cria um novo perfil de beneficiário após validar:
        /// - E-mail único
        /// - Contacto único
        /// </remarks>
        /// <param name="beneficiaryPostDto">Dados do novo beneficiário</param>
        /// <returns>Resultado da operação</returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [Produces("application/json")]
        [HttpPost()]
        [AllowAnonymous]
        public async Task<IActionResult> PostBeneficiary([FromBody] BeneficiaryPostDTO beneficiaryPostDto)
        {
            try
            {
                //Busca id do user a efetuar o registo.
                int userId = (int)HttpContext.Items["UserId"];

                if (await _dbContext.Users.AnyAsync(p => p.Email == beneficiaryPostDto.Email))
                    return BadRequest(new Resposta("Este e-mail já está registado."));

                if (await _dbContext.Users.AnyAsync(p => p.Contact == beneficiaryPostDto.Contact))
                    return BadRequest(new Resposta("Este contacto já está registado."));

                // Use the main context for adding the new user and address
                var beneficiary = new Beneficiary
                {
                    Name = beneficiaryPostDto.Name,
                    Email = beneficiaryPostDto.Email,
                    Contact = beneficiaryPostDto.Contact,
                    Course = beneficiaryPostDto.Course,
                    CurricularYear = beneficiaryPostDto.CurricularYear,
                    GlobalObs = beneficiaryPostDto.GlobalObs,
                    CreatedBy = userId,
                    Address = new BeneficiaryAddress
                    {
                        Street = beneficiaryPostDto.Street,
                        PostalCode = beneficiaryPostDto.PostalCode,
                        Number = beneficiaryPostDto.Number
                    }
                };


                await _dbContext.Beneficiaries.AddAsync(beneficiary);
                await _dbContext.SaveChangesAsync();

                return Ok(new Resposta("Perfil criado com sucesso."));
            }
            catch (Exception ex)
            {
                return BadRequest(new Resposta("Erro ao criar perfil. Tente novamente. " + ex.Message));
            }
        }


        /// <summary>
        /// Atualizar perfil de beneficiário.
        /// </summary>
        /// <remarks>
        /// Atualiza os dados de perfil de beneficiário existente,
        /// incluindo informações de morada.
        /// </remarks>
        /// <param name="beneficiaryId">ID do beneficiário a atualizar</param>
        /// <param name="beneficiaryPutDto">Novos dados para o beneficiário</param>
        /// <returns>Resultado da operação</returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [Produces("application/json")]
        [HttpPut("{beneficiaryId}")]
        public async Task<IActionResult> PutBeneficiary(int beneficiaryId, [FromBody] BeneficiaryPutDTO beneficiaryPutDto)
        {
            try
            {
                int userId = (int)HttpContext.Items["UserId"];

                // Buscar beneficiário existente
                var beneficiary = await _dbContext.Beneficiaries
                    .Include(b => b.Address)
                    .Include(b => b.ParticularObs)
                    .FirstOrDefaultAsync(b => b.Id == beneficiaryId);

                if (beneficiary == null)
                    return NotFound(new Resposta("Beneficiário não encontrado."));

                // Verificar duplicações de e-mail e contacto (mas ignorar o próprio beneficiário)
                if (await _dbContext.Beneficiaries.AnyAsync(p => p.Email == beneficiaryPutDto.Email && p.Id != beneficiaryId))
                    return BadRequest(new Resposta("Este e-mail já está registado."));

                if (await _dbContext.Beneficiaries.AnyAsync(p => p.Contact == beneficiaryPutDto.Contact && p.Id != beneficiaryId))
                    return BadRequest(new Resposta("Este contacto já está registado."));

                // Atualizar os campos principais
                beneficiary.Name = beneficiaryPutDto.Name;
                beneficiary.Email = beneficiaryPutDto.Email;
                beneficiary.Contact = beneficiaryPutDto.Contact;
                beneficiary.Course = beneficiaryPutDto.Course;
                beneficiary.CurricularYear = beneficiaryPutDto.CurricularYear;
                beneficiary.GlobalObs = beneficiaryPutDto.GlobalObs;

                // Atualizar ou criar morada
                if (beneficiary.Address == null)
                {
                    beneficiary.Address = new BeneficiaryAddress
                    {
                        Street = beneficiaryPutDto.Street,
                        Number = beneficiaryPutDto.Number,
                        PostalCode = beneficiaryPutDto.PostalCode
                    };
                }
                else
                {
                    beneficiary.Address.Street = beneficiaryPutDto.Street;
                    beneficiary.Address.Number = beneficiaryPutDto.Number;
                    beneficiary.Address.PostalCode = beneficiaryPutDto.PostalCode;
                }

                // Faz o upsert da observação particular
                if (!string.IsNullOrWhiteSpace(beneficiaryPutDto.ParticularObs))
                {
                    var obs = beneficiary.ParticularObs
                        .FirstOrDefault(o => o.UserId == userId);

                    if (obs != null)
                    {
                        obs.Obs = beneficiaryPutDto.ParticularObs;
                    }
                    else
                    {
                        beneficiary.ParticularObs.Add(new ParticularOb
                        {
                            UserId = userId,
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


        /// <summary>
        /// Busca todos os beneficiários existentes consoante filtros.
        /// </summary>
        /// <remarks>
        /// <param name="pageNumber">Número da página (começa em 1)</param>
        /// <param name="pageSize">Quantidade de itens por página (máx. 50)</param>
        /// <param name="orderBy">Ordenação Alfabética ("asc" = Ascendente, "desc" = Descendente</param>
        /// <param name="searchTerm">Termo para busca por nome</param>
        /// <returns>Lista paginada de beneficiários</returns>
        [HttpGet()]
        public async Task<ActionResult<PaginatedResponse<BeneficiaryListDTO>>> GetProfiles(
             [FromQuery] int pageNumber = 1,
             [FromQuery] int pageSize = 10,
             [FromQuery] string orderBy = "asc",
             [FromQuery] string searchTerm = "")
        {
            try
            {
                // Validação dos parâmetros
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
                    return NotFound(new Resposta("Nenhum beneficiário encontrado com os filtros e termo de pesquisa."));
                }

                // Aplica paginação
                var totalCount = beneficiaries.Count;
                var pagedBeneficiaries = beneficiaries
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Cria resposta paginada
                var paginatedResponse = new PaginatedResponse<BeneficiaryListDTO>
                {
                    Data = pagedBeneficiaries,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                if (!paginatedResponse.Data.Any())
                {
                    return NotFound(new Resposta("Página solicitada está vazia."));
                }

                return Ok(paginatedResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                                  new Resposta("Ocorreu um erro interno ao obter os beneficiários."));
            }
        }


        /// <summary>
        /// Busca um beneficiário específico.
        /// </summary>
        /// <param name="beneficiaryId">ID do beneficiário a consultar</param>
        /// <returns>Detalhes de um beneficiário</returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<BeneficiaryGetDTO>))]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BeneficiaryGetDTO))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [Produces("application/json")]
        [HttpGet("{beneficiaryId}")]
        public async Task<ActionResult<Resposta>> GetProfile(int beneficiaryId)
        {
            try
            {
                var beneficiary = await _dbContext.Beneficiaries
                    .Where(p => p.Id == beneficiaryId)
                    .Select(p => new BeneficiaryGetDTO
                    {
                       Id = p.Id,
                       Name = p.Name,
                       Email = p.Email,
                       Contact = p.Contact,
                       Nif = p.Nif,
                       Course = p.Course,
                       CurricularYear = p.CurricularYear,
                       StudentNum = p.StudentNum
                    })
                        .FirstOrDefaultAsync();

                    if (beneficiary == null)
                        return NotFound(new Resposta("Perfil não encontrado."));

                return Ok(beneficiary);
                
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao obter o perfil."));
            }
        }


    }
}