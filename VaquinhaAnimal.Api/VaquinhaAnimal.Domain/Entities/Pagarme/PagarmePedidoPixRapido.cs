using System.Collections.Generic;

namespace VaquinhaAnimal.Domain.Entities.Pagarme
{
    public class PagarmePedidoPixRapido
    {
        public PagarmePedidoPixRapidoCustomer customer { get; set; }
        public List<PagarmePedidoItensPixRapido> items { get; set; }
        public List<PagarmePedidoPagamentosPixRapido> payments { get; set; }
        public double valorPlataforma { get; set; }
        public string recaptcha { get; set; }
    }

    public class PagarmePedidoPixRapidoCustomer
    {
        public string name { get; set; }
        public string email { get; set; }
        public string document { get; set; }
        public string type { get; set; }
        public PagarmePedidoPixRapidoPhones phones { get; set; }
    }

    public class PagarmePedidoPixRapidoPhones
    {
        public PagarmePedidoPixRapidoMobilePhone mobile_phone { get; set; }
    }

    public class PagarmePedidoPixRapidoMobilePhone
    {
        public string country_code { get; set; }
        public string area_code { get; set; }
        public string number { get; set; }
    }

    public class PagarmePedidoItensPixRapido
    {
        public double amount { get; set; }
        public string description { get; set; }
        public int quantity { get; set; }
        public string code { get; set; }
    }

    public class PagarmePedidoPagamentosPixRapido
    {
        public string payment_method { get; set; }
        public PagarmePedidoPixRapidoJson pix { get; set; }
        public List<PagarmePedidoPixRapidoSplit> split { get; set; }
    }

    public class PagarmePedidoPixRapidoSplit
    {
        public int amount { get; set; }
        public string recipient_id { get; set; }
        public string type { get; set; }
        public PagarmeSplitPixRapidoOptions options { get; set; }
    }

    public class PagarmeSplitPixRapidoOptions
    {
        public bool charge_processing_fee { get; set; }
        public bool charge_remainder_fee { get; set; }
        public bool liable { get; set; }
    }

    public class PagarmePedidoPixRapidoJson
    {
        public int expires_in { get; set; }
    }
}