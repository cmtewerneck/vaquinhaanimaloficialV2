using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Domain.Entities.Validations;
using VaquinhaAnimal.Domain.Interfaces;
using System;
using System.Threading.Tasks;

namespace VaquinhaAnimal.Domain.Services
{
    public class CartaoService : BaseService, ICartaoService
    {
        private readonly ICartaoRepository _cartaoRepository;

        public CartaoService(ICartaoRepository cartaoRepository,
                               INotificador notificador) : base(notificador)
        {
            _cartaoRepository = cartaoRepository;
        }

        public async Task<bool> Adicionar(Cartao cartao)
        {
            if (!ExecutarValidacao(new CartaoValidation(), cartao)) return false;

            await _cartaoRepository.Insert(cartao);
            return true;
        }

        public async Task<bool> Atualizar(Cartao cartao)
        {
            if (!ExecutarValidacao(new CartaoValidation(), cartao)) return false;

            await _cartaoRepository.Update(cartao);

            return true;
        }

        public async Task<bool> Remover(Guid id)
        {
            await _cartaoRepository.Delete(id);
            return true;
        }

        public void Dispose()
        {
            _cartaoRepository?.Dispose();
        }
    }
}
