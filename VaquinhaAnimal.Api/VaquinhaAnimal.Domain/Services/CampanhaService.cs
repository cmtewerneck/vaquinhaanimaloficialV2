using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Domain.Entities.Validations;
using VaquinhaAnimal.Domain.Interfaces;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace VaquinhaAnimal.Domain.Services
{
    public class CampanhaService : BaseService, ICampanhaService
    {
        private readonly ICampanhaRepository _campanhaRepository;

        public CampanhaService(ICampanhaRepository campanhaRepository,
                               INotificador notificador) : base(notificador)
        {
            _campanhaRepository = campanhaRepository;
        }

        public async Task<bool> Adicionar(Campanha campanha)
        {
            if (!ExecutarValidacao(new CampanhaValidation(), campanha)) return false;

            if (_campanhaRepository.Buscar(f => f.Titulo == campanha.Titulo).Result.Any())
            {
                Notificar("Já existe uma campanha com este título.");
                return false;
            }

            await _campanhaRepository.Insert(campanha);
            return true;
        }

        public async Task<bool> Atualizar(Campanha campanha)
        {
            if (!ExecutarValidacao(new CampanhaValidation(), campanha)) return false;

            if (_campanhaRepository.Buscar(f => f.Titulo == campanha.Titulo && f.Id != campanha.Id).Result.Any())
            {
                Notificar("Já existe uma campanha com este título.");
                return false;
            }

            await _campanhaRepository.Update(campanha);

            return true;
        }

        public async Task<bool> Remover(Guid id)
        {
            if (_campanhaRepository.GetByIdWithImagesAndDonationsAsync(id).Result.Doacoes.Any())
            {
                // ALTERAR STATUS PARA INATIVA
                return false;
            }

            await _campanhaRepository.Delete(id);
            return true;
        }

        public void Dispose()
        {
            _campanhaRepository?.Dispose();
        }
    }
}
