using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VaquinhaAnimal.Data.Context;
using VaquinhaAnimal.Data.Repositories;
using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Domain.Interfaces;

namespace VaquinhaAnimal.Data.Repository
{
    public class CartaoRepository : Repository<Cartao>, ICartaoRepository
    {
        public CartaoRepository(VaquinhaDbContext context) : base(context) { }
       
        public async Task<List<Cartao>> GetAllCardsAsync(string customerId)
        {
            var result = await Db.Cartoes
                .AsNoTracking()
                .Where(p => p.Customer_Id == customerId)
                .ToListAsync();

            return result;
        }

    }
}
