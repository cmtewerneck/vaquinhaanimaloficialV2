using System.Threading.Tasks;
using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Domain.Helpers;

namespace VaquinhaAnimal.Domain.Interfaces
{
    public interface IArtigoRepository : IRepository<Artigo>
    {
        // TESTE DE PAGINAÇÃO
        Task<PagedResult<Artigo>> ListAsync(int _PageSize, int _PageNumber);
        Task<Artigo> GetByUrl(string url_artigo);
    }
}
