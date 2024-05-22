using VaquinhaAnimal.Domain.Entities.Validations;
using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Domain.Interfaces;
using System;
using System.Threading.Tasks;

namespace VaquinhaAnimal.Domain.Services
{
    public class DoacaoService : BaseService, IDoacaoService
    {
        private readonly IDoacaoRepository _doacaoRepository;

        public DoacaoService(IDoacaoRepository doacaoRepository,
                              INotificador notificador) : base(notificador)
        {
            _doacaoRepository = doacaoRepository;
        }

        public async Task<bool> Adicionar(Doacao doacao)
        {
            if (!ExecutarValidacao(new DoacaoValidation(), doacao)) return false;

            await _doacaoRepository.Insert(doacao);
            return true;
        }

        public async Task<bool> Atualizar(Doacao doacao)
        {
            if (!ExecutarValidacao(new DoacaoValidation(), doacao)) return false;

            await _doacaoRepository.Update(doacao);
            return true;
        }

        public async Task<bool> Remover(Guid id)
        {
            await _doacaoRepository.Delete(id);
            return true;
        }

        public void Dispose()
        {
            _doacaoRepository?.Dispose();
        }
    }
}
