using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace VaquinhaAnimal.Domain.Entities.Base
{
    public class SignalRHub : Hub, ISignalR
    {
        private readonly IHubContext<SignalRHub> _hub;

        public SignalRHub(IHubContext<SignalRHub> hub)
        {
            _hub = hub;
        }

        public async Task PixIsPaid(bool _isPaid)
        {
            await _hub.Clients.All.SendAsync("pixIsPaid", _isPaid);
        }
    }
}
 