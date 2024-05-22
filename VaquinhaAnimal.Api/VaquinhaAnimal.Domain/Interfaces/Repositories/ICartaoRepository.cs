using System.Collections.Generic;
using System.Threading.Tasks;
using VaquinhaAnimal.Domain.Entities;

namespace VaquinhaAnimal.Domain.Interfaces
{
    public interface ICartaoRepository : IRepository<Cartao>
    {
        Task<List<Cartao>> GetAllCardsAsync(string customerId);
    }
}
