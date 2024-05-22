using System;
using System.Collections.Generic;

namespace VaquinhaAnimal.Domain.Entities.Pagarme
{
    public class WebhookPedidos
    {
        public string id { get; set; }
        public AccountPedidos account { get; set; }
        public string type { get; set; }
        public DateTime created_at { get; set; }
        public DataPedidos data { get; set; }
    }

    public class AccountPedidos
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class DataPedidos
    {
        public string id { get; set; }
        public string code { get; set; }
        public decimal amount { get; set; }
        //public int amount { get; set; }
        public string currency { get; set; }
        public bool closed { get; set; }
        public List<ItemPedidos> items { get; set; }
        public CustomerPedidos customer { get; set; }
        public string status { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public DateTime closed_at { get; set; }
        public List<ChargePedidos> charges { get; set; }
    }

    public class ChargePedidos
    {
        public string id { get; set; }
        public string code { get; set; }
        public string gateway_id { get; set; }
        public int amount { get; set; }
        public int paid_amount { get; set; }
        public string status { get; set; }
        public string currency { get; set; }
        public string payment_method { get; set; }
        public DateTime paid_at { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public CustomerPedidos2 customer { get; set; }
    }

    public class ItemPedidos
    {
        public string id { get; set; }
        public string type { get; set; }
        public string description { get; set; }
        public int amount { get; set; }
        public int quantity { get; set; }
        public string status { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string code { get; set; }
    }

    public class BillingAddressPedidos
    {
        public string zip_code { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string line_1 { get; set; }
        public string line_2 { get; set; }
    }

    public class CustomerPedidos
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
        public PhonesPedidos phones { get; set; }
        public BillingAddressPedidos address { get; set; }

    }

    public class CustomerPedidos2
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
        public PhonesPedidos phones { get; set; }
        public BillingAddressPedidos address { get; set; }
        public LastTransactionPedidos last_transaction { get; set; }

    }

    public class LastTransactionPedidos  
    {
        public string id { get; set; }
        public string transaction_type { get; set; }
        public string gateway_id { get; set; }
        public int amount { get; set; }
        public string status { get; set; }
        public bool success { get; set; }
        public int installments { get; set; }
        public string statement_descriptor { get; set; }
        public string acquirer_name { get; set; }
        public string acquirer_tid { get; set; }
        public string acquirer_nsu { get; set; }
        public string acquirer_auth_code { get; set; }
        public string acquirer_return_code { get; set; }
        public string operation_type { get; set; }
        public CardPedidos card { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public GatewayResponsePedidos gateway_response { get; set; }
        public AntifraudResponsePedidos antifraud_response { get; set; }
        public string metadata { get; set; }
    }

    public class GatewayResponsePedidos
    {
        public string code { get; set; }
        public string[] errors { get; set; }
    }

    public class AntifraudResponsePedidos
    {
        public string status { get; set; }
        public string score { get; set; }
        public string provider_name { get; set; }
    }

    public class CardPedidos
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
        public BillingAddressPedidos billing_address { get; set; }
    }

    public class PhonesPedidos
    {
        public MobilePhonePedidos mobile_phone { get; set; }
    }

    public class MobilePhonePedidos
    {
        public string country_code { get; set; }
        public string number { get; set; }
        public string area_code { get; set; }
    }
}