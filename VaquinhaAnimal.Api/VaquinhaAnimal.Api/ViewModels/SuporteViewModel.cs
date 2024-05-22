using System;

namespace VaquinhaAnimal.Api.ViewModels
{
    public class SuporteViewModel
    {
        public Guid id { get; set; }
        public DateTime data { get; set; }
        public Guid usuario_id { get; set; }
        public string assunto { get; set; }
        public string mensagem { get; set; }
        public string resposta { get; set; }
        public bool respondido { get; set; }
    }
}
