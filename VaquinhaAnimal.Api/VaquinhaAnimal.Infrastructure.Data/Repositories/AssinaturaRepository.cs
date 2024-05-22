using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using VaquinhaAnimal.Data.Context;
using VaquinhaAnimal.Data.Repositories;
using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Domain.Interfaces;

namespace VaquinhaAnimal.Data.Repository
{
    public class AssinaturaRepository : Repository<Assinatura>, IAssinaturaRepository
    {
        public AssinaturaRepository(VaquinhaDbContext context) : base(context) { }

        public async Task<Assinatura> GetBySubscriptionAsync(string subscriptionId)
        {
            return await Db.Assinaturas
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.SubscriptionId == subscriptionId);
        }
    }
}
