using System.Text.Json.Serialization;

namespace VaquinhaAnimal.Domain.Entities.Pagarme
{
    public class PagarmeCard
    {
        [JsonPropertyName("number")]
        public string Number { get; set; }

        [JsonPropertyName("holder_name")]
        public string Holder_Name { get; set; }

        [JsonPropertyName("holder_document")]
        public string Holder_Document { get; set; }

        [JsonPropertyName("exp_month")]
        public int Exp_Month { get; set; }

        [JsonPropertyName("exp_year")]
        public int Exp_Year { get; set; }

        [JsonPropertyName("cvv")]
        public string CVV { get; set; }

        [JsonPropertyName("brand")]
        public string Brand { get; set; }

        [JsonPropertyName("billing_address")]
        public PagarmeCardBillingAddress Billing_Address { get; set; }

        [JsonPropertyName("options")]
        public PagarmeCardOptions Options { get; set; }

        public PagarmeCard()
        {
            this.Options = new PagarmeCardOptions();
        }
    }

    public class PagarmeCardBillingAddress
    {
        [JsonPropertyName("line_1")]
        public string Line_1 { get; set; }

        [JsonPropertyName("line_2")]
        public string Line_2 { get; set; }

        [JsonPropertyName("zip_code")]
        public string Zip_Code { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }
    }

    public class PagarmeCardOptions
    {
        [JsonPropertyName("verify_card")]
        public bool Verify_Card { get; set; }

        public PagarmeCardOptions()
        {
            this.Verify_Card = true;
        }
    }
}