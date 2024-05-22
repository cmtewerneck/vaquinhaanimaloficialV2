using System.Collections.Generic;

namespace VaquinhaAnimal.Domain.Entities.Pagarme
{
    public class PagarmePedidoRecorrencia
    {
        public string recaptcha { get; set; }
        public string payment_method { get; set; }
        public string interval { get; set; }
        public int interval_count { get; set; }
        public string billing_type { get; set; }
        public int installments { get; set; }
        public string statement_descriptor { get; set; }
        public string currency { get; set; }
        public string card_id { get; set; }
        public PagarmeAssinaturaCustomer customer { get; set; }
        public List<RecorrenciaItem> items { get; set; }
        public PagarmeAssianturaCard card { get; set; }
        public PagarmeAssinaturaSplit split { get; set; }
    }

    public class PagarmeAssinaturaSplit
    {
        public bool enabled { get; set; }
        public List<PagarmeAssinaturaRulesSplit> rules { get; set; }
    }

    public class PagarmeAssinaturaRulesSplit
    {
        public int amount { get; set; }
        public string recipient_id { get; set; }
        public string type { get; set; }
        public PagarmeAssinaturaSplitOptions options { get; set; }
    }

    public class PagarmeAssinaturaSplitOptions
    {
        public bool charge_processing_fee { get; set; }
        public bool charge_remainder_fee { get; set; }
        public bool liable { get; set; }
    }

    public class PagarmeAssianturaCard
    {
        public string number { get; set; }
        public string holder_name { get; set; }
        public string holder_document { get; set; }
        public int? exp_month { get; set; }
        public int? exp_year { get; set; }
        public string cvv { get; set; }
        public BillingAddressAssiantura billing_address { get; set; }
    }

    public class BillingAddressAssiantura
    {
        public string line_1 { get; set; }
        public string zip_code { get; set; }
        public string state { get; set; }
        public string city { get; set; }
        public string country { get; set; }
    }

    public class PagarmeAssinaturaCustomer
    {
        public string name { get; set; }
        public string email { get; set; }
        public string document { get; set; }
        public string type { get; set; }
        public PagarmeAssinaturaPhones phones { get; set; }
    }

    public class PagarmeAssinaturaPhones
    {
        public PagarmeAssinaturaMobilePhone mobile_phone { get; set; }
    }

    public class PagarmeAssinaturaMobilePhone
    {
        public string country_code { get; set; }
        public string area_code { get; set; }
        public string number { get; set; }
    }

    public class RecorrenciaItem
    {
        public string description { get; set; }
        public int quantity { get; set; }
        public RecorrenciaScheme pricing_scheme { get; set; }
    }

    public class RecorrenciaScheme
    {
        public string scheme_type { get; set; }
        public int price { get; set; }
    }
}