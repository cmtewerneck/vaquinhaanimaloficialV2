using VaquinhaAnimal.Data.Context;
using VaquinhaAnimal.Data.Repositories;
using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VaquinhaAnimal.Data.Repository
{
    public class SuporteRepository : Repository<Suporte>, ISuporteRepository
    {
        public SuporteRepository(VaquinhaDbContext context) : base(context) { }
       
        public async Task<List<Suporte>> GetAllMyTicketsAsync(Guid usuario_id)
        {
            var result = await Db.Suportes
                .AsNoTracking()
                .Where(p => p.Usuario_Id == usuario_id)
                .ToListAsync();

            return result;
        }

        public async Task<List<Suporte>> GetAllTicketsAsync()
        {
            var result = await Db.Suportes
                .AsNoTracking()
                .ToListAsync();

            return result;
        }

    }
}
