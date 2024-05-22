using System;

namespace VaquinhaAnimal.Domain.Entities.Pagarme
{
    public class WebhookCartoes
    {
        public string id { get; set; }
        public Account account { get; set; }
        public string type { get; set; }
        public DateTime created_at { get; set; }
        public Data data { get; set; }
    }

    public class Account
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class Data
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
        public DateTime deleted_at { get; set; }
        public BillingAddress billing_address { get; set; }
        public Customer customer { get; set; }
    }

    public class BillingAddress
    {
        public string zip_code { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string line_1 { get; set; }
        public string line_2 { get; set; }
    }

    public class Customer
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
        public Phones phones { get; set; }
    }

    public class Phones
    {
        public MobilePhone mobile_phone { get; set; }
    }

    public class MobilePhone
    {
        public string country_code { get; set; }
        public string number { get; set; }
        public string area_code { get; set; }
    }
}