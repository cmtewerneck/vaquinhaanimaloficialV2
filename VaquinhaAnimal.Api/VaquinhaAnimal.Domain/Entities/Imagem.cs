using VaquinhaAnimal.Domain.Entities.Base;
using VaquinhaAnimal.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VaquinhaAnimal.Domain.Entities
{
    public class Imagem : BaseEntity
    {
        [JsonPropertyName("tipo")]
        [Required]
        public TipoImagemEnum Tipo { get; set; } 

        [JsonPropertyName("arquivo")]
        [Required]
        [MaxLength(500)]
        public string Arquivo { get; set; } 

        [JsonPropertyName("campanha_id")]
        [Required]
        public Guid Campanha_Id { get; set; }

        [JsonIgnore]
        public Campanha Campanha { get; set; }
    }
}
