using System;
using System.Text.Json.Serialization;

namespace VaquinhaAnimal.Domain.Entities.Pagarme
{
    public class PagarmeCliente
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("document")]
        public string Document { get; set; }
    }
}