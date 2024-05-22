using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using VaquinhaAnimal.Domain.Entities.Base;

namespace VaquinhaAnimal.Domain.Entities
{
    public class Beneficiario : BaseEntity
    {
        [JsonPropertyName("nome")]
        [Required]
        public string Nome { get; set; }

        [JsonPropertyName("documento")]
        [Required]
        [MaxLength(16)]
        [MinLength(11)]
        public string Documento { get; set; }

        [JsonPropertyName("tipo")]
        [Required]
        public string Tipo { get; set; }

        [JsonPropertyName("codigo_banco")]
        [Required]
        public string CodigoBanco { get; set; }

        [JsonPropertyName("numero_agencia")]
        [Required]
        public string NumeroAgencia { get; set; }

        [JsonPropertyName("digito_agencia")]
        public string DigitoAgencia { get; set; }

        [JsonPropertyName("numero_conta")]
        [Required]
        public string NumeroConta { get; set; }

        [JsonPropertyName("digito_conta")]
        [Required]
        public string DigitoConta { get; set; }

        [JsonPropertyName("tipo_conta")]
        [Required]
        public string TipoConta { get; set; }

        [JsonPropertyName("recebedor_id")]
        public string RecebedorId { get; set; }

        [JsonPropertyName("campanha_id")]
        [Required]
        public Guid Campanha_Id { get; set; }
        public Campanha Campanha { get; set; }
    }
}
