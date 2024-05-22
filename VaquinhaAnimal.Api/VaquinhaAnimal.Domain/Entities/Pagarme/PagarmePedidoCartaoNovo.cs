using System.Collections.Generic;

namespace VaquinhaAnimal.Domain.Entities.Pagarme
{
    public class PagarmePedidoCartaoNovo
    {
        public List<PagarmePedidoItensCartaoNovo> items { get; set; }
        public string customer_id { get; set; }
        public List<PagarmePedidoPagamentosCartaoNovo> payments { get; set; }
        public bool salvarCartao { get; set; }
    }

    public class PagarmePedidoItensCartaoNovo
    {
        public double amount { get; set; }
        //public int amount { get; set; }
        public string description { get; set; }
        public int quantity { get; set; }
        public string code { get; set; }
    }

    public class PagarmePedidoPagamentosCartaoNovo
    {
        public string payment_method { get; set; }
        public PagarmePedidoCartaoCreditoCartaoNovo credit_card { get; set; }
        public List<PagarmePedidoCartaoCreditoCartaoNovoSplit> split { get; set; }
    }

    public class PagarmePedidoCartaoCreditoCartaoNovoSplit
    {
        public int amount { get; set; }
        public string recipient_id { get; set; }
        public string type { get; set; }
        public PagarmePedidoCartaoCreditoCartaoNovoOptions options { get; set; }
    }

    public class PagarmePedidoCartaoCreditoCartaoNovoOptions
    {
        public bool charge_processing_fee { get; set; }
        public bool charge_remainder_fee { get; set; }
        public bool liable { get; set; }
    }

    public class PagarmePedidoCartaoCreditoCartaoNovo
    {
        public bool recurrence { get; set; }
        public int installments { get; set; }
        public string statement_descriptor { get; set; }
        public PagarmePedidoCartaoCreditoNovo card { get; set; }
    }

    public class PagarmePedidoCartaoCreditoNovo
    {
        public string number { get; set; }
        public int exp_month { get; set; }
        public int exp_year { get; set; }
        public string cvv { get; set; }
        public string holder_name { get; set; }
        public string holder_document { get; set; }
        public BillingAddressCartaoNovo billing_address { get; set; }
    }

    public class BillingAddressCartaoNovo
    {
        public string line_1 { get; set; }
        public string zip_code { get; set; }
        public string state { get; set; }
        public string city { get; set; }
        public string country { get; set; }
    }
}