using System.Collections.Generic;

namespace VaquinhaAnimal.Api.ViewModels
{
    public class AssinaturaCreateViewModel
    {
        public string recaptcha { get; set; }
        public string card_id { get; set; }
        public PagarmeAssinaturaCustomerVM customer { get; set; }
        public List<RecorrenciaItemVM> items { get; set; }
        public PagarmeAssianturaCardVM card { get; set; }
    }

    public class PagarmeAssianturaCardVM
    {
        public string number { get; set; }
        public string holder_name { get; set; }
        public string holder_document { get; set; }
        public int? exp_month { get; set; }
        public int? exp_year { get; set; }
        public string cvv { get; set; }
        public BillingAddressAssianturaVM billing_address { get; set; }
    }

    public class BillingAddressAssianturaVM
    {
        public string line_1 { get; set; }
        public string zip_code { get; set; }
        public string state { get; set; }
        public string city { get; set; }
        public string country { get; set; }
    }

    public class PagarmeAssinaturaCustomerVM
    {
        public string name { get; set; }
        public string email { get; set; }
        public string document { get; set; }
        public string type { get; set; }
    }

    public class RecorrenciaItemVM
    {
        public string description { get; set; }
        public int quantity { get; set; }
        public RecorrenciaSchemeVM pricing_scheme { get; set; }
    }

    public class RecorrenciaSchemeVM
    {
        public string scheme_type { get; set; }
        public int price { get; set; }
    }
}

