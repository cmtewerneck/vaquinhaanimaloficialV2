using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using VaquinhaAnimal.Data.Context;
using VaquinhaAnimal.Data.Repositories;
using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Domain.Helpers;
using VaquinhaAnimal.Domain.Interfaces;

namespace VaquinhaAnimal.Data.Repository
{
    public class AdocaoRepository : Repository<Adocao>, IAdocaoRepository
    {
        public AdocaoRepository(VaquinhaDbContext context) : base(context) { }

        // TESTE DE PAGINAÇÃO
        public async Task<PagedResult<Adocao>> ListAsync(int _PageSize, int _PageNumber)
        {
            var totalResults = Db.Adocoes
                .AsNoTracking()
                .Where(c => c.Adotado == false)
                .Count();

            var result = await Db.Adocoes
                .AsNoTracking()
                .Where(c => c.Adotado == false)
                .OrderBy(p => p.NomePet)
                .Skip((_PageNumber - 1) * _PageSize)
                .Take(_PageSize)
                .ToListAsync();

            var resultPaginado = new PagedResult<Adocao>
            {
                PageNumber = _PageNumber,
                PageSize = _PageSize,
                TotalRecords = totalResults,
                Data = result
            };

            return resultPaginado;
        }

        public async Task<PagedResult<Adocao>> ListMyAdocoesAsync(int _PageSize, int _PageNumber, Guid userId)
        {
            var totalResults = Db.Adocoes
                .AsNoTracking()
                .Where(c => c.UsuarioId == userId.ToString())
                .Count();

            var result = await Db.Adocoes
                .AsNoTracking()
                .Where(c => c.UsuarioId == userId.ToString())
                .Skip((_PageNumber - 1) * _PageSize)
                .Take(_PageSize)
                .OrderBy(p => p.NomePet)
                .ToListAsync();

            var resultPaginado = new PagedResult<Adocao>
            {
                PageNumber = _PageNumber,
                PageSize = _PageSize,
                TotalRecords = totalResults,
                Data = result
            };

            return resultPaginado;
        }

        public async Task<Adocao> GetByUrl(string url_adocao)
        {
            return await Db.Adocoes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UrlAdocao == url_adocao);
        }
    }
}
