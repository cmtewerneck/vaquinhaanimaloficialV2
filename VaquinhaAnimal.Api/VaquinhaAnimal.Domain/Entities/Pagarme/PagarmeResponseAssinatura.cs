using System;
using System.Collections.Generic;

namespace VaquinhaAnimal.Domain.Entities.Pagarme
{
    public class PagarmeResponseAssinatura
    {
        public string type { get; set; }
        public DateTime created_at { get; set; }
        public ResponseData data { get; set; }
    }

    public class ResponseData
    {
        public string id { get; set; }
        public int paid_amount { get; set; }
        public string status { get; set; }
        public string payment_method { get; set; }
        public ResponseInvoice invoice { get; set; }
        public ResponseCustomer customer { get; set; }
        public ResponseLastTransaction last_transaction { get; set; }
    }

    public class ResponseInvoice
    {
        public string id { get; set; }
        public string subscriptionId { get; set; }
    }

    public class ResponseCustomer
    {
        public string id { get; set; }
    }

    public class ResponseLastTransaction
    {
        public string id { get; set; }
        public List<ResponseSplit> split { get; set; }
        public CardAssinaturaResponse card { get; set; }
    }

    public class CardAssinaturaResponse
    {
        public string holder_document { get; set; }
    }

    public class ResponseSplit
    {
        public int amount { get; set; }
    }
}