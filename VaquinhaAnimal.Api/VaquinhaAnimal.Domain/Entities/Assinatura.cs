using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using VaquinhaAnimal.Domain.Entities.Base;

namespace VaquinhaAnimal.Domain.Entities
{
    public class Assinatura : BaseEntity
    {
        [JsonPropertyName("subscription_id")]
        [Required]
        public string SubscriptionId { get; set; }

        [JsonPropertyName("campanha_id")]
        [Required]
        public Guid CampanhaId { get; set; }
        public Campanha Campanha { get; set; }
    }
}
