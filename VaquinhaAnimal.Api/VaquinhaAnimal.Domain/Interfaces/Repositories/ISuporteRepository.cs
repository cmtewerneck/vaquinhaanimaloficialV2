using VaquinhaAnimal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VaquinhaAnimal.Domain.Interfaces
{
    public interface ISuporteRepository : IRepository<Suporte>
    {
        Task<List<Suporte>> GetAllMyTicketsAsync(Guid usuario_id);
        Task<List<Suporte>> GetAllTicketsAsync();
    }
}
