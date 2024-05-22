using System;
using System.Threading.Tasks;
using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Domain.Entities.Validations;
using VaquinhaAnimal.Domain.Interfaces;

namespace VaquinhaAnimal.Domain.Services
{
    public class AdocaoService : BaseService, IAdocaoService
    {
        private readonly IAdocaoRepository _adocaoRepository;

        public AdocaoService(IAdocaoRepository adocaoRepository,
                             INotificador notificador) : base(notificador)
        {
            _adocaoRepository = adocaoRepository;
        }

        public async Task<bool> Adicionar(Adocao adocao)
        {
            if (!ExecutarValidacao(new AdocaoValidation(), adocao)) return false;
            try
            {
                await _adocaoRepository.Insert(adocao);
            }
            catch (Exception ex)
            {
                Notificar("dsdasad " + ex);
                throw;
            }
            
            //await _adocaoRepository.Insert(adocao);
            return true;
        }

        public async Task<bool> Atualizar(Adocao adocao)
        {
            if (!ExecutarValidacao(new AdocaoValidation(), adocao)) return false;

            await _adocaoRepository.Update(adocao);
            return true;
        }

        public async Task<bool> Remover(Guid id)
        {
            await _adocaoRepository.Delete(id);
            return true;
        }

        public void Dispose()
        {
            _adocaoRepository?.Dispose();
        }
    }
}
