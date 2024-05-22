using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VaquinhaAnimal.Domain.Entities.Base;

namespace VaquinhaAnimal.Domain.Interfaces
{
    public interface IRepository<TEntity> : IDisposable where TEntity : BaseEntity
    {
        Task<TEntity> GetByIdAsync(Guid id);
        Task<List<TEntity>> GetAllAsync();
        Task<int> ObterTotalRegistros();
        Task<List<TEntity>> Buscar(Expression<Func<TEntity, bool>> predicate);

        Task Insert(TEntity entity);
        Task Delete(Guid id);
        Task Update(TEntity entity);

        Task<int> SaveChangesAsync();
    }
}
