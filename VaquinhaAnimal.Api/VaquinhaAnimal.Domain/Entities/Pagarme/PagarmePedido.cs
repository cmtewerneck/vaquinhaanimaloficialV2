using System.Collections.Generic;

namespace VaquinhaAnimal.Domain.Entities.Pagarme
{
    public class PagarmePedido
    {
        public string recaptcha { get; set; }
        public PagarmePedidoCustomer customer { get; set; }
        public List<PagarmePedidoItens> items { get; set; }
        public List<PagarmePedidoPagamentos> payments { get; set; }
        public double valorPlataforma { get; set; }
    }

    public class PagarmePedidoCustomer
    {
        public string name { get; set; }
        public string email { get; set; }
        public string document { get; set; }
        public string type { get; set; }
        public PagarmePedidoPhones phones { get; set; }
    }

    public class PagarmePedidoPhones
    {
        public PagarmePedidoMobilePhone mobile_phone { get; set; }
    }

    public class PagarmePedidoMobilePhone
    {
        public string country_code { get; set; }
        public string area_code { get; set; }
        public string number { get; set; }
    }

    public class PagarmePedidoItens
    {
        public double amount { get; set; }
        public string description { get; set; }
        public int quantity { get; set; }
        public string code { get; set; }
    }

    public class PagarmePedidoPagamentos
    {
        public string payment_method { get; set; }
        public PagarmePedidoCartaoCredito credit_card { get; set; }
        public List<PagarmePedidoSplit> split { get; set; }
    }

    public class PagarmePedidoCartaoCredito
    {
        public bool recurrence { get; set; }
        public int installments { get; set; }
        public string statement_descriptor { get; set; }
        public string card_id { get; set; }
        public PagarmePedidoCartaoCreditoUsado card { get; set; }
    }

    public class PagarmePedidoSplit
    {
        public int amount { get; set; }
        public string recipient_id { get; set; }
        public string type { get; set; }
        public PagarmeSplitOptions options { get; set; }
    }

    public class PagarmeSplitOptions
    {
        public bool charge_processing_fee { get; set; }
        public bool charge_remainder_fee { get; set; }
        public bool liable { get; set; }
    }

    public class PagarmePedidoCartaoCreditoUsado
    {
        public string number { get; set; }
        public int? exp_month { get; set; }
        public int? exp_year { get; set; }
        public string cvv { get; set; }
        public string holder_name { get; set; }
        public string holder_document { get; set; }
        public BillingAddressCartaoNovo billing_address { get; set; }
    }

    public class PagarmePedidoBillingAddress
    {
        public string line_1 { get; set; }
        public string zip_code { get; set; }
        public string state { get; set; }
        public string city { get; set; }
        public string country { get; set; }
    }
}