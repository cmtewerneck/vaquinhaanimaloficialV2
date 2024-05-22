using VaquinhaAnimal.Domain.Entities.Base;
using System;
using System.Text.Json.Serialization;

namespace VaquinhaAnimal.Domain.Entities
{
    public class Suporte : BaseEntity
    {
        [JsonPropertyName("data")]
        public DateTime Data { get; set; } 

        [JsonPropertyName("usuario_id")]
        public Guid Usuario_Id { get; set; }

        [JsonPropertyName("assunto")]
        public string Assunto { get; set; }

        [JsonPropertyName("mensagem")]
        public string Mensagem { get; set; }

        [JsonPropertyName("resposta")]
        public string Resposta { get; set; }

        [JsonPropertyName("respondido")]
        public bool Respondido { get; set; }
    }    
}