using VaquinhaAnimal.Domain.Notificacoes;
using System.Collections.Generic;

namespace VaquinhaAnimal.Domain.Interfaces
{
    public interface INotificador
    {
        bool TemNotificacao();
        List<Notificacao> ObterNotificacoes();
        void Handle(Notificacao notificacao);
    }
}
