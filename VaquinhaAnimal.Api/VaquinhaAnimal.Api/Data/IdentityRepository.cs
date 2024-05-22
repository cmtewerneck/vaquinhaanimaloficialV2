using VaquinhaAnimal.Domain.Entities.Base;
using VaquinhaAnimal.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace VaquinhaAnimal.Api.Data
{
    public class IdentityRepository : IIdentityRepository
    {
        private ApplicationDbContext context;
        private DbSet<ApplicationUser> _db;

        public IdentityRepository(ApplicationDbContext context)
        {
            this.context = context;
            _db = context.Set<ApplicationUser>();
        }

        public string GetCodigoPagarme(string id)
        {
            ApplicationUser usuario =  _db.SingleOrDefault(s => s.Id == id);
            return usuario.Codigo_Pagarme;
        }
    }
}