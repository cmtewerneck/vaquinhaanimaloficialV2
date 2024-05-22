using System;
using System.Linq;
using System.Threading.Tasks;
using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Domain.Interfaces;

namespace VaquinhaAnimal.Domain.Services
{
    public class ArtigoService : BaseService, IArtigoService
    {
        private readonly IArtigoRepository _artigoRepository;

        public ArtigoService(IArtigoRepository artigoRepository,
                            INotificador notificador) : base(notificador)
        {
            _artigoRepository = artigoRepository;
        }

        public async Task<bool> Adicionar(Artigo artigo)
        {
            if (_artigoRepository.Buscar(f => f.Titulo == artigo.Titulo).Result.Any())
            {
                Notificar("Já existe um artigo com este título.");
                return false;
            }

            await _artigoRepository.Insert(artigo);
            return true;
        }

        public async Task<bool> Atualizar(Artigo artigo)
        {
            if (_artigoRepository.Buscar(f => f.Titulo == artigo.Titulo && f.Id != artigo.Id).Result.Any())
            {
                Notificar("Já existe um artigo com este título.");
                return false;
            }

            await _artigoRepository.Update(artigo);

            return true;
        }

        public async Task<bool> Remover(Guid id)
        {
            await _artigoRepository.Delete(id);
            return true;
        }

        public void Dispose()
        {
            _artigoRepository?.Dispose();
        }
    }
}
