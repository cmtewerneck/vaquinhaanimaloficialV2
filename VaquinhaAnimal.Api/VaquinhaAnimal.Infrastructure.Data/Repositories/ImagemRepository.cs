using VaquinhaAnimal.Data.Context;
using VaquinhaAnimal.Data.Repositories;
using VaquinhaAnimal.Domain.Entities;
using VaquinhaAnimal.Domain.Interfaces;

namespace VaquinhaAnimal.Data.Repository
{
    public class ImagemRepository : Repository<Imagem>, IImagemRepository
    {
        public ImagemRepository(VaquinhaDbContext context) : base(context) { }
    }
}
