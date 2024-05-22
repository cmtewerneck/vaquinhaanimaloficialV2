using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using VaquinhaAnimal.Data.Context;
using VaquinhaAnimal.Data.Repositories;
using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Domain.Helpers;
using VaquinhaAnimal.Domain.Interfaces;

namespace VaquinhaAnimal.Data.Repository
{
    public class ArtigoRepository : Repository<Artigo>, IArtigoRepository
    {
        public ArtigoRepository(VaquinhaDbContext context) : base(context) { }

        // TESTE DE PAGINAÇÃO
        public async Task<PagedResult<Artigo>> ListAsync(int _PageSize, int _PageNumber)
        {
            var totalResults = Db.Artigos
                .AsNoTracking()
                .Count();

            var result = await Db.Artigos
                .AsNoTracking()
                .Skip((_PageNumber - 1) * _PageSize)
                .Take(_PageSize)
                .ToListAsync();

            var resultPaginado = new PagedResult<Artigo>
            {
                PageNumber = _PageNumber,
                PageSize = _PageSize,
                TotalRecords = totalResults,
                Data = result
            };

            return resultPaginado;
        }
        public async Task<Artigo> GetByUrl(string url_artigo)
        {
            return await Db.Artigos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UrlArtigo == url_artigo);
        }
    }
}
