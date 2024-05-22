using System;

namespace VaquinhaAnimal.Api.ViewModels
{
    public class DoacaoViewModel
    {
        public Guid id { get; set; }
        public DateTime data { get; set; }
        public decimal valor { get; set; }
        public string forma_pagamento { get; set; }
        public string status { get; set; }
        public string transacao_id { get; set; }
        public string url_download { get; set; }
        public string usuario_id { get; set; }
        public string customer_id { get; set; }
        public string charge_id { get; set; }
        public Guid campanha_id { get; set; } 
        public CampanhaViewModel Campanha { get; set; }
    }
}
