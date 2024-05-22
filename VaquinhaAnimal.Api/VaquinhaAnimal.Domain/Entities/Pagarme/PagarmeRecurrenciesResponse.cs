using System;
using System.Collections.Generic;

namespace VaquinhaAnimal.Domain.Entities.Pagarme
{
    public class PagarmeRecurrenciesResponse
    {
        public string id { get; set; }
        public int billing_day { get; set; }
        public DateTime start_at { get; set; }
        public DateTime? canceled_at { get; set; }
        public string status { get; set; }
        public List<ItemsAssinatura> items { get; set; }
    }

    public class ItemsAssinatura
    {
        public PricingAssinaturas pricing_scheme { get; set; }
    }

    public class PricingAssinaturas
    {
        public int price { get; set; }
    }
}