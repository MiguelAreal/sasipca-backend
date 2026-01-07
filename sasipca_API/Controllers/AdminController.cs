using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sasipca_API.Attributes;
using sasipca_API.DBModels;
using sasipca_API.Dtos;
using sasipca_API.Models;
using static sasipca_API.Enumerators.Enums;

namespace sasipca_API.Controllers
{
    [Route("api/admins")]
    [ApiController]
    [AuthorizeRole(UserRole.Admin)]
    public class AdminController : ControllerBase
    {
        private readonly SasipcaContext _context;

        public AdminController(SasipcaContext context)
        {
            _context = context;
        }

        // GET: api/admins
        // Lista administradores com Paginação e Pesquisa
        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<AdminListDto>>> GetAdmins(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string searchTerm = "")
        {
            try
            {
                // 1. Validação de parâmetros
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 10;

                var searchTermLower = searchTerm?.ToLower() ?? string.Empty;

                // 2. Query Base
                var query = _context.Users.AsQueryable();

                // 3. Filtragem (Nome, Email ou Contacto)
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(u =>
                        (u.Name != null && u.Name.ToLower().Contains(searchTermLower)) ||
                        u.Email.ToLower().Contains(searchTermLower) ||
                        u.Contact.Contains(searchTermLower));
                }

                // 4. Ordenação (Default: Nome)
                query = query.OrderBy(u => u.Name ?? u.Email); // Ordena por email se nome for null

                // 5. Contagem Total (para a paginação)
                var totalCount = await query.CountAsync();

                // 6. Projeção e Paginação
                var pagedAdmins = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new AdminListDto
                    {
                        Id = u.Id,
                        Name = u.Name ?? "Pendente de Login",
                        Email = u.Email!,
                        Contact = u.Contact
                    })
                    .ToListAsync();

                // 7. Resposta
                var paginatedResponse = new PaginatedResponse<AdminListDto>
                {
                    Data = pagedAdmins,
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
                    new Resposta($"Erro ao listar administradores: {ex.Message}"));
            }
        }

        // POST: api/admins
        // Cria um novo administrador
        [HttpPost]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateUserDto createDto)
        {
            if (string.IsNullOrEmpty(createDto.Email) || !createDto.Email.EndsWith("ipca.pt"))
                return BadRequest(new Resposta("O email deve ser válido e do domínio ipca.pt."));

            // Validar se já existe email
            if (await _context.Users.AnyAsync(u => u.Email == createDto.Email))
                return BadRequest(new Resposta("Já existe um administrador com este email."));

            // Validar se já existe contacto
            if (await _context.Users.AnyAsync(u => u.Contact == createDto.Contact))
                return BadRequest(new Resposta("Já existe um administrador com este contacto."));

            try
            {
                var newUser = new User
                {
                    Email = createDto.Email,
                    Contact = createDto.Contact
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetAdmins), new { id = newUser.Id }, new Resposta("Administrador criado com sucesso."));
            }
            catch (Exception ex)
            {
                return BadRequest(new Resposta($"Erro ao criar admin: {ex.Message}"));
            }
        }
    }
}