using System;
using System.Collections.Generic;

namespace VaquinhaAnimal.Domain.Entities.Pagarme
{
    public class PagarmePedidoBoleto
    {
        public PagarmePedidoBoletoCustomer customer { get; set; }
        public List<PagarmePedidoItensBoleto> items { get; set; }
        public List<PagarmePedidoPagamentosBoleto> payments { get; set; }
        public double valorPlataforma { get; set; }
        public string recaptcha { get; set; }
    }

    public class PagarmePedidoBoletoCustomer
    {
        public string name { get; set; }
        public string email { get; set; }
        public string document { get; set; }
        public string type { get; set; }
        public PagarmePedidoBoletoPhones phones { get; set; }
    }

    public class PagarmePedidoBoletoPhones
    {
        public PagarmePedidoBoletoMobilePhone mobile_phone { get; set; }
    }

    public class PagarmePedidoBoletoMobilePhone
    {
        public string country_code { get; set; }
        public string area_code { get; set; }
        public string number { get; set; }
    }

    public class PagarmePedidoItensBoleto
    {
        public double amount { get; set; }
        public string description { get; set; }
        public int quantity { get; set; }
        public string code { get; set; }
    }

    public class PagarmePedidoPagamentosBoleto
    {
        public string payment_method { get; set; }
        public PagarmePedidoBoletoJson boleto { get; set; }
        public List<PagarmePedidoBoletoSplit> split { get; set; }
    }

    public class PagarmePedidoBoletoSplit
    {
        public int amount { get; set; }
        public string recipient_id { get; set; }
        public string type { get; set; }
        public PagarmeSplitBoletoOptions options { get; set; }
    }

    public class PagarmeSplitBoletoOptions
    {
        public bool charge_processing_fee { get; set; }
        public bool charge_remainder_fee { get; set; }
        public bool liable { get; set; }
    }

    public class PagarmePedidoBoletoJson
    {
        public string instructions { get; set; }
        public DateTime? due_at { get; set; }
    }
}