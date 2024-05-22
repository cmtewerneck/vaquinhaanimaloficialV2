using System;
using System.Collections.Generic;

namespace VaquinhaAnimal.Domain.Entities.Pagarme
{
    public class WebhookAssinaturas
    {
        public string id { get; set; }
        public AccountAssinaturas account { get; set; }
        public string type { get; set; }
        public DateTime created_at { get; set; }
        public DataAssinaturas data { get; set; }
    }

    public class AccountAssinaturas
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class DataAssinaturas
    {
        public string id { get; set; }
        public string code { get; set; }
        public DateTime start_at { get; set; }
        public string interval { get; set; }
        public int interval_count { get; set; }
        public string billing_type { get; set; }
        public int billing_day { get; set; }
        public CurrentCycleAssinaturas current_cycle { get; set; }
        public DateTime next_billing_at { get; set; }
        public string payment_method { get; set; }
        public string currency { get; set; }
        public int installments { get; set; }
        public string status { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public DateTime canceled_at { get; set; }
        public CustomerAssinaturas customer { get; set; }
        public CardAssinaturas card { get; set; }
        public List<ItemAssinaturas> items { get; set; }
    }

    public class CardAssinaturas
    {
        public string id { get; set; }
        public string first_six_digits { get; set; }
        public string last_four_digits { get; set; }
        public string brand { get; set; }
        public string holder_name { get; set; }
        public string holder_document { get; set; }
        public int exp_month { get; set; }
        public int exp_year { get; set; }
        public string status { get; set; }
        public string type { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public BillingAddressAssinaturas billing_address { get; set; }
    }

    public class CurrentCycleAssinaturas
    {
        public string id { get; set; }
        public DateTime start_at { get; set; }
        public DateTime end_at { get; set; }
        public DateTime billing_at { get; set; }
        public string status { get; set; }
        public int cycle { get; set; }
    }

    public class ItemAssinaturas
    {
        public string id { get; set; }
        public string description { get; set; }
        public int quantity { get; set; }
        public string status { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public PricingSchemeAssinaturas pricing_scheme { get; set; }
    }

    public class PricingSchemeAssinaturas
    {
        public int price { get; set; }
        public string scheme_type { get; set; }
    }

    public class BillingAddressAssinaturas
    {
        public string zip_code { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string line_1 { get; set; }
        public string line_2 { get; set; }
    }

    public class CustomerAssinaturas
    {
        public string id { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string code { get; set; }
        public string document { get; set; }
        public string document_type { get; set; }
        public string type { get; set; }
        public string gender { get; set; }
        public bool delinquent { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public DateTime birthdate { get; set; }
        public PhonesAssinaturas phones { get; set; }
        public BillingAddressAssinaturas address { get; set; }

    }

    public class PhonesAssinaturas
    {
        public MobilePhoneAssinaturas mobile_phone { get; set; }
    }

    public class MobilePhoneAssinaturas
    {
        public string country_code { get; set; }
        public string number { get; set; }
        public string area_code { get; set; }
    }
}