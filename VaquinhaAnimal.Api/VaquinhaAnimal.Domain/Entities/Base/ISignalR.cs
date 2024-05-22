using System.Threading.Tasks;

namespace VaquinhaAnimal.Domain.Entities.Base
{
    public interface ISignalR
    {
        Task PixIsPaid(bool isPaid);
    }
}
