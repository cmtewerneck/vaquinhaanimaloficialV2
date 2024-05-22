using VaquinhaAnimal.Domain.Entities.Base;
using System;
using System.Threading.Tasks;

namespace VaquinhaAnimal.Domain.Interfaces
{
    public interface IBaseService<TEntity> : IDisposable where TEntity : BaseEntity
    {
        Task<bool> Adicionar(TEntity entity);
        Task<bool> Atualizar(TEntity entity);
        Task<bool> Remover(Guid id);
    }
}
