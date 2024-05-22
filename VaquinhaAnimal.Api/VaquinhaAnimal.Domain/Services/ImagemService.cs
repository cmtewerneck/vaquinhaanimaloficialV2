using VaquinhaAnimal.Domain.Entities.Validations;
using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Domain.Interfaces;
using System;
using System.Threading.Tasks;

namespace VaquinhaAnimal.Domain.Services
{
    public class ImagemService : BaseService, IImagemService
    {
        private readonly IImagemRepository _imagemRepository;

        public ImagemService(IImagemRepository imagemRepository,
                              INotificador notificador) : base(notificador)
        {
            _imagemRepository = imagemRepository;
        }

        public async Task<bool> Adicionar(Imagem imagem)
        {
            if (!ExecutarValidacao(new ImagemValidation(), imagem)) return false;

            await _imagemRepository.Insert(imagem);
            return true;
        }

        public async Task<bool> Atualizar(Imagem imagem)
        {
            if (!ExecutarValidacao(new ImagemValidation(), imagem)) return false;

            await _imagemRepository.Update(imagem);
            return true;
        }

        public async Task<bool> Remover(Guid id)
        {
            await _imagemRepository.Delete(id);
            return true;
        }

        public void Dispose()
        {
            _imagemRepository?.Dispose();
        }
    }
}
