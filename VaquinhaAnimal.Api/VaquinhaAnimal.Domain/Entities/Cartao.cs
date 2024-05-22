using VaquinhaAnimal.Domain.Entities.Base;
using System.Text.Json.Serialization;

namespace VaquinhaAnimal.Domain.Entities
{
    public class Cartao : BaseEntity
    {
        [JsonPropertyName("card_id")]
        public string Card_Id { get; set; } 

        [JsonPropertyName("customer_id")]
        public string Customer_Id { get; set; }

        [JsonPropertyName("first_six_digits")]
        public string First_Six_Digits { get; set; }

        [JsonPropertyName("last_four_digits")]
        public string Last_Four_Digits { get; set; }

        [JsonPropertyName("exp_month")]
        public int Exp_Month { get; set; }

        [JsonPropertyName("exp_year")]
        public int Exp_Year { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }    
}