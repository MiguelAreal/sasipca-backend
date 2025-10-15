using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sasipca_API.Models;
using sasipca_API.Services;
using Microsoft.AspNetCore.Authorization;
using sasipca_API.Data;
using sasipca_API.Dtos;
using sasipca_API.Dtos.sasipca_API.Dtos;

namespace sasipca_API.Controllers
{
    /// <summary>
    /// Controller para gestão de pessoas
    /// </summary>
    [Route("api/pessoa")]
    [ApiController]
    [Authorize]
    public class PessoaController : ControllerBase
    {
        private readonly NLDbContext _dbContext;
        private readonly AuthService _authService;
        private readonly JWTService _jwtService;

        /// <summary>
        /// Inicialização do PessoaController
        /// </summary>
        /// <param name="authService">Serviço de autenticação</param>
        /// <param name="jwtService">Serviço JWT</param>
        /// <param name="context">Contexto da base de dados</param>
        public PessoaController(AuthService authService, JWTService jwtService, NLDbContext context)
        {
            _dbContext = context;
            _authService = authService;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Busca uma pessoa específica.
        /// </summary>
        /// <remarks>
        /// Apenas é possível consultar pessoas que partilhem o mesmo código postal.
        /// </remarks>
        /// <param name="pessoaId">ID da pessoa a consultar</param>
        /// <returns>Lista de pessoas ou detalhes de uma pessoa</returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<PessoaGetDTO>))]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PessoaGetDTO))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [Produces("application/json")]
        [HttpGet("{pessoaId}")]
        public async Task<ActionResult<Resposta>> BuscarPessoas(int pessoaId)
        {
            try
            {
                var userId = (int)HttpContext.Items["UserId"];
                string? codPostalPessoaAutenticada = "5555";

                var pessoa = await _dbContext.Pessoa
                    .Where(p => p.IdPessoa == pessoaId)
                    .Select(p => new PessoaGetDTO
                    {
                        IdPessoa = p.IdPessoa,
                        Nome = p.Nome,
                        Morada = p.Morada,
                        Email = p.Email,
                        Contacto = p.Contacto,
                        CodigoPostal = p.IdCodPostal,
                        DataCriacao = p.DataCriacao
                    })
                        .FirstOrDefaultAsync();

                    if (pessoa == null)
                        return NotFound(new Resposta("Pessoa não encontrada."));

                    if (pessoa.CodigoPostal != codPostalPessoaAutenticada)
                        return Unauthorized(new Resposta("Você não tem acesso a este recurso."));

                    var mediaAvaliacoes = await _dbContext.Servico
                        .Where(s => s.IdExecutor == pessoa.IdPessoa && s.IdAvaliacao != null)
                        .Select(s => s.IdAvaliacaoNavigation.Nota)
                        .ToListAsync();

                    pessoa.MediaAvaliacoes = mediaAvaliacoes.Any() ? mediaAvaliacoes.Average() : 0;

                    // Número total de anúncios feitos pela pessoa
                    var produtosCount = await _dbContext.Produto
                        .CountAsync(p => p.IdVendedor == pessoa.IdPessoa);

                    var servicosCount = await _dbContext.Servico
                        .CountAsync(s => s.IdCriador == pessoa.IdPessoa);

                    var eventosCount = await _dbContext.Evento
                        .CountAsync(e => e.IdCriador == pessoa.IdPessoa);

                    pessoa.NumeroAnuncios = produtosCount + servicosCount + eventosCount;

                return Ok(pessoa);
                
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao obter o perfil."));
            }
        }

        /// <summary>
        /// Retorna todos os anúncios criados por um utilizador.
        /// </summary>
        /// <remarks>
        /// Apenas é possível consultar os próprios anúncios criados.
        /// </remarks>
        /// <param name="pagina">Número da página (default: 1)</param>
        /// <param name="itensPorPagina">Número de itens por página (default: 10)</param>
        /// <returns>Lista de anúncios criados pela pessoa</returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginacaoDTO<AnuncioResumoDTO>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [Produces("application/json")]
        [HttpGet("anuncios")]
        public async Task<ActionResult<PaginacaoDTO<AnuncioResumoDTO>>> getAnuncios(
            [FromQuery] int pagina = 1,
            [FromQuery] int itensPorPagina = 10)
        {
            try
            {
                var userId = (int)HttpContext.Items["UserId"];

                // Consulta para produtos
                var queryProdutos = _dbContext.Produto
                    .Where(p => p.IdVendedor == userId)
                    .Select(p => new AnuncioResumoDTO
                    {
                        Id = p.IdProduto,
                        Nome = p.Nome,
                        Tipo = "Produto",
                        Estado = p.IdEstadoNavigation.TipoEstado,
                        DataCriacao = p.DataCriacao
                    });

                // Consulta para serviços
                var queryServicos = _dbContext.Servico
                    .Where(s => s.IdCriador == userId)
                    .Select(s => new AnuncioResumoDTO
                    {
                        Id = s.IdServico,
                        Nome = s.Nome,
                        Tipo = "Servico",
                        Estado = s.IdEstadoNavigation.TipoEstado,
                        DataCriacao = s.DataCriacao
                    });

                // Consulta para eventos
                var queryEventos = _dbContext.Evento
                    .Where(e => e.IdCriador == userId)
                    .Select(e => new AnuncioResumoDTO
                    {
                        Id = e.IdEvento,
                        Nome = e.Nome,
                        Tipo = "Evento",
                        Estado = e.IdEstadoNavigation.TipoEstado,
                        DataCriacao = e.DataCriacao
                    });

                // Unir todas as consultas
                var queryUnida = queryProdutos.Concat(queryServicos).Concat(queryEventos);

                // Calcular o total de itens
                var totalItens = await queryUnida.CountAsync();

                // Aplicar paginação
                var anuncios = await queryUnida
                    .OrderByDescending(a => a.DataCriacao)
                    .Skip((pagina - 1) * itensPorPagina)
                    .Take(itensPorPagina)
                    .ToListAsync();

                // Calcular o total de páginas
                var totalPaginas = (int)Math.Ceiling(totalItens / (double)itensPorPagina);

                // Retornar o resultado paginado
                return Ok(new PaginacaoDTO<AnuncioResumoDTO>
                {
                    Itens = anuncios,
                    PaginaAtual = pagina,
                    ItensPorPagina = itensPorPagina,
                    TotalItens = totalItens,
                    TotalPaginas = totalPaginas
                });
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao buscar os anúncios"));
            }
        }

        /// <summary>
        /// Retorna todas as atividades (propostas e participações) de um utilizador.
        /// </summary>
        /// <remarks>
        /// Apenas é possível consultar as próprias atividades.
        /// </remarks>
        /// <param name="pagina">Número da página (default: 1)</param>
        /// <param name="itensPorPagina">Número de itens por página (default: 10)</param>
        /// <returns>Lista de atividades do utilizador</returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginacaoDTO<AtividadeResumoDTO>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [Produces("application/json")]
        [HttpGet("atividades")]
        public async Task<ActionResult<PaginacaoDTO<AtividadeResumoDTO>>> getAtividades(
            [FromQuery] int pagina = 1,
            [FromQuery] int itensPorPagina = 10)
        {
            try
            {
                var userId = (int)HttpContext.Items["UserId"];

                // Consulta para propostas de produto
                var queryPropostasProduto = _dbContext.PropostaProduto
                    .Where(pp => pp.IdComprador == userId)
                    .Include(pp => pp.IdProdutoNavigation)
                    .Include(pp => pp.IdEstadoNavigation)
                    .Select(pp => new AtividadeResumoDTO
                    {
                        Id = pp.IdPropostaProduto,
                        Nome = pp.IdProdutoNavigation.Nome,
                        Tipo = "Proposta a Produto",
                        Estado = pp.IdEstadoNavigation.TipoEstado,
                        DataCriacao = pp.IdProdutoNavigation.DataCriacao,
                        IdOriginal = pp.IdProduto
                    });

                // Consulta para propostas de serviço
                var queryPropostasServico = _dbContext.PropostaServico
                    .Where(ps => ps.IdExecutor == userId)
                    .Include(ps => ps.IdServicoNavigation)
                    .Include(ps => ps.IdEstadoNavigation)
                    .Select(ps => new AtividadeResumoDTO
                    {
                        Id = ps.IdPropostaServico,
                        Nome = ps.IdServicoNavigation.Nome,
                        Tipo = "Proposta a Serviço",
                        Estado = ps.IdEstadoNavigation.TipoEstado,
                        DataCriacao = ps.IdServicoNavigation.DataCriacao,
                        IdOriginal = ps.IdServico
                    });

                // Consulta para participações em eventos
                var queryParticipacoesEventos = _dbContext.InscricaoEvento
                    .Where(ie => ie.IdPessoa == userId)
                    .Include(ie => ie.IdEventoNavigation)
                    .Select(ie => new AtividadeResumoDTO
                    {
                        Id = ie.IdInscricao,
                        Nome = ie.IdEventoNavigation.Nome,
                        Tipo = "Inscrições a Eventos",
                        Estado = "Confirmada",
                        DataCriacao = ie.DataInscricao,
                        IdOriginal = ie.IdEvento
                    });

                // Unir todas as consultas
                var queryUnida = queryPropostasProduto
                    .Concat(queryPropostasServico)
                    .Concat(queryParticipacoesEventos);

                // Calcular o total de itens
                var totalItens = await queryUnida.CountAsync();

                // Aplicar paginação
                var atividades = await queryUnida
                    .OrderByDescending(a => a.DataCriacao)
                    .Skip((pagina - 1) * itensPorPagina)
                    .Take(itensPorPagina)
                    .ToListAsync();

                // Calcular o total de páginas
                var totalPaginas = (int)Math.Ceiling(totalItens / (double)itensPorPagina);

                // Retornar o resultado paginado
                return Ok(new PaginacaoDTO<AtividadeResumoDTO>
                {
                    Itens = atividades,
                    PaginaAtual = pagina,
                    ItensPorPagina = itensPorPagina,
                    TotalItens = totalItens,
                    TotalPaginas = totalPaginas
                });
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao buscar as atividades"));
            }
        }



        #region Métodos Auxiliares Privados

        private async Task<double> CalcularMediaAvaliacoes(int pessoaId)
        {
            var avaliacoes = await _dbContext.Servico
                .Where(s => s.IdExecutor == pessoaId && s.IdAvaliacao != null)
                .Select(s => s.IdAvaliacaoNavigation.Nota)
                .ToListAsync();

            return avaliacoes.Any() ? avaliacoes.Average() : 0;
        }

        #endregion
    }
}