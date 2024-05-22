using System;
using System.Threading.Tasks;
using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Domain.Helpers;

namespace VaquinhaAnimal.Domain.Interfaces
{
    public interface IAdocaoRepository : IRepository<Adocao>
    {
        // TESTE DE PAGINAÇÃO
        Task<PagedResult<Adocao>> ListAsync(int _PageSize, int _PageNumber);
        Task<PagedResult<Adocao>> ListMyAdocoesAsync(int _PageSize, int _PageNumber, Guid userId);
        Task<Adocao> GetByUrl(string url_adocao);
    }
}
