using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sasipca_API.Models;
using Microsoft.AspNetCore.Authorization;
using sasipca_API.Services;
using sasipca_API.Services.Interfaces;
using sasipca_API.Enumerators;
using sasipca_API.Dtos;
using sasipca_API.DBModels;

namespace sasipca_API.Controllers
{
    /// <summary>
    /// Controller para listas de dados (relação ID - Nome para tipos, status, etc).
    /// </summary>
    [Route("api/lists")]
    [ApiController]
    public class ListController : ControllerBase
    {
        private readonly SasipcaContext _dbContext;
        private readonly INotificationService _notifService;
        private readonly IAuthService _authService;
        private readonly ImageProcessingService _imageProcessingService;
        private readonly IProductService _productService;

        /// <summary>
        /// Inicialização do ListController
        /// </summary>
        public ListController(SasipcaContext context)
        {
            _dbContext = context;
        }

        /// <returns>Lista de categorias</returns>
        /// <summary>
        /// Busca as listas de categorias e tipos de unidade para produtos.
        /// </summary>
        /// <returns>Objeto contendo as listas.</returns>
        [HttpGet()]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ListsGetDTO))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [Produces("application/json")]
        public async Task<ActionResult<ListsGetDTO>> GetProductLists()
        {
            try
            {
                // Busca categorias
                var categorias = await _dbContext.CategoryTypes
                    .Select(c => new CategoryTypes
                    {
                        Id = c.Id,
                        Type = c.Type
                    }).ToListAsync();

                // Busca tipos de unidade
                var unidades = await _dbContext.UnitTypes
                    .Select(u => new UnitTypes
                    {
                        Id = u.Id,
                        Type = u.Type
                    }).ToListAsync();

                // Busca tipos de movimentos
                var movimentos = await _dbContext.MovementTypes
                    .Select(u => new MovementTypes
                    {
                        Id = u.Id,
                        Type = u.Type
                    }).ToListAsync();

                // Busca status de entregas
                var entregas = await _dbContext.DeliveryStatuses
                    .Select(u => new DeliveriesStatus
                    {
                        Id = u.Id,
                        Status = u.Status
                    }).ToListAsync();

                // Busca tipos de relatórios
                var relatórios = await _dbContext.ReportTypes
                    .Select(u => new ReportTypes
                    {
                        Id = u.Id,
                        Type = u.Type
                    }).ToListAsync();

                var lists = new ListsGetDTO
                {
                    Categories = categorias,
                    Units = unidades,
                    Movements = movimentos,
                    Deliveries = entregas,
                    Reports = relatórios
                };

                return Ok(lists);
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao obter as listas."));
            }
        }

    } 
}