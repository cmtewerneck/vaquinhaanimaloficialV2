using System;
using System.Threading.Tasks;
using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Domain.Interfaces;

namespace VaquinhaAnimal.Domain.Services
{
    public class AssinaturaService : BaseService, IAssinaturaService
    {
        private readonly IAssinaturaRepository _assinaturaRepository;

        public AssinaturaService(IAssinaturaRepository assinaturaRepository,
                               INotificador notificador) : base(notificador)
        {
            _assinaturaRepository = assinaturaRepository;
        }

        public async Task<bool> Adicionar(Assinatura assinatura)
        {
            await _assinaturaRepository.Insert(assinatura);
            return true;
        }

        public async Task<bool> Atualizar(Assinatura assinatura)
        {
            await _assinaturaRepository.Update(assinatura);
            return true;
        }

        public async Task<bool> Remover(Guid id)
        {
            await _assinaturaRepository.Delete(id);
            return true;
        }

        public void Dispose()
        {
            _assinaturaRepository?.Dispose();
        }
    }
}
