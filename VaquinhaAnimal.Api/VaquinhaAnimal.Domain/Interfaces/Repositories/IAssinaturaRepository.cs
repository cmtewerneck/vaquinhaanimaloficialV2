using System.Threading.Tasks;
using VaquinhaAnimal.Domain.Entities;

namespace VaquinhaAnimal.Domain.Interfaces
{
    public interface IAssinaturaRepository : IRepository<Assinatura>
    {
        Task<Assinatura> GetBySubscriptionAsync(string subscriptionId);
    }
}
