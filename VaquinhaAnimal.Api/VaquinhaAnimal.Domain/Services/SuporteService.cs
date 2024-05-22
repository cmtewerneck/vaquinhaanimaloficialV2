using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Domain.Entities.Validations;
using VaquinhaAnimal.Domain.Interfaces;
using System;
using System.Threading.Tasks;

namespace VaquinhaAnimal.Domain.Services
{
    public class SuporteService : BaseService, ISuporteService
    {
        private readonly ISuporteRepository _suporteRepository;

        public SuporteService(ISuporteRepository suporteRepository,
                               INotificador notificador) : base(notificador)
        {
            _suporteRepository = suporteRepository;
        }

        public async Task<bool> Adicionar(Suporte suporte)
        {
            if (!ExecutarValidacao(new SuporteValidation(), suporte)) return false;

            await _suporteRepository.Insert(suporte);
            return true;
        }

        public async Task<bool> Atualizar(Suporte suporte)
        {
            if (!ExecutarValidacao(new SuporteValidation(), suporte)) return false;

            await _suporteRepository.Update(suporte);

            return true;
        }

        public async Task<bool> Remover(Guid id)
        {
            await _suporteRepository.Delete(id);
            return true;
        }

        public void Dispose()
        {
            _suporteRepository?.Dispose();
        }
    }
}
