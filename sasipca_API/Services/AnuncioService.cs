// Services/AnuncioService.cs
using Microsoft.EntityFrameworkCore;
using sasipca_API.Data;
using sasipca_API.Dtos;
using sasipca_API.Enumerators;
using sasipca_API.Services.Interfaces;

namespace sasipca_API.Services
{
    public class AnuncioService : IAnuncioService
    {
        private readonly NLDbContext _dbcontext;

        public AnuncioService(NLDbContext context)
        {
            _dbcontext = context;
        }

        public async Task<List<AnuncioListaDTO>> ObterAnuncios(string userPostalCode, string searchTerm, int? userId = null)
        {
            var searchTermLower = searchTerm?.ToLower() ?? string.Empty;

            var produtos = _dbcontext.Produto
                .Where(p => p.IdEstado == (int)Enums.EstadoProduto.Criado &&
                            p.IdVendedorNavigation.IdCodPostal == userPostalCode &&
                            (string.IsNullOrEmpty(searchTerm) || p.Nome.ToLower().Contains(searchTermLower)) &&
                            (!userId.HasValue || p.IdVendedor == userId.Value))
                .Select(p => new AnuncioListaDTO
                {
                    Id = p.IdProduto,
                    Nome = p.Nome,
                    Preco = p.Preco,
                    TipoPreco = "Total",
                    Categoria = "Produto",
                    ImagemUrl = p.IdImagem.OrderBy(i => i.IdImagem).Select(i => i.Url).FirstOrDefault(),
                    DataCriacao = p.DataCriacao
                });

            var servicos = _dbcontext.Servico
                .Where(s => s.IdEstado == (int)Enums.EstadoProduto.Criado &&
                            s.IdCriadorNavigation.IdCodPostal == userPostalCode &&
                            (string.IsNullOrEmpty(searchTerm) || s.Nome.ToLower().Contains(searchTermLower)) &&
                            (!userId.HasValue || s.IdCriador == userId.Value))
                .Select(s => new AnuncioListaDTO
                {
                    Id = s.IdServico,
                    Nome = s.Nome,
                    Preco = s.Preco,
                    TipoPreco = s.IdModalidadeprecoNavigation.Tipo,
                    Categoria = "Servico",
                    ImagemUrl = s.IdImagem.OrderBy(i => i.IdImagem).Select(i => i.Url).FirstOrDefault(),
                    DataCriacao = s.DataCriacao
                });

            var eventos = _dbcontext.Evento
                .Where(e => e.IdEstado == (int)Enums.EstadoProduto.Criado &&
                            e.IdCriadorNavigation.IdCodPostal == userPostalCode &&
                            (string.IsNullOrEmpty(searchTerm) || e.Nome.ToLower().Contains(searchTermLower)) &&
                            (!userId.HasValue || e.IdCriador == userId.Value))
                .Select(e => new AnuncioListaDTO
                {
                    Id = e.IdEvento,
                    Nome = e.Nome,
                    Preco = null,
                    TipoPreco = null,
                    Categoria = "Evento",
                    ImagemUrl = null,
                    DataCriacao = e.DataCriacao
                });

            return await produtos.Concat(servicos).Concat(eventos).ToListAsync();
        }
    }
}