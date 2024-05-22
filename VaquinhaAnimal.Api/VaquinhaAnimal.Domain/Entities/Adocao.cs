using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using VaquinhaAnimal.Domain.Entities.Base;
using VaquinhaAnimal.Domain.Enums;

namespace VaquinhaAnimal.Domain.Entities
{
    public class Adocao : BaseEntity
    {
        [JsonPropertyName("nome_pet")]
        [Required]
        public string NomePet { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("celular")]
        public string Celular { get; set; }

        [JsonPropertyName("instagram")]
        public string Instagram { get; set; }

        [JsonPropertyName("facebook")]
        public string Facebook { get; set; }

        [JsonPropertyName("tipo_pet")]
        [Required]
        public TipoPetEnum TipoPet { get; set; }

        [JsonPropertyName("faixa_etaria")]
        [Required]
        public FaixaEtariaEnum FaixaEtaria { get; set; }

        [JsonPropertyName("url_adocao")]
        [Required]
        public string UrlAdocao { get; set; } // (200)

        [JsonPropertyName("castrado")]
        [Required]
        public bool Castrado { get; set; }

        [JsonPropertyName("descricao")]
        public string Descricao { get; set; }

        [JsonPropertyName("tipo_anunciante")]
        [Required]
        public TipoAnuncianteEnum TipoAnunciante { get; set; }

        [JsonPropertyName("abrigo_nome")]
        public string Abrigo_Nome { get; set; }

        [JsonPropertyName("empresa_nome")]
        public string Empresa_Nome { get; set; }

        [JsonPropertyName("particular_nome")]
        public string Particular_Nome { get; set; }

        [JsonPropertyName("adotado")]
        [Required]
        public bool Adotado { get; set; }

        [JsonPropertyName("foto")]
        public string Foto { get; set; }

        [JsonPropertyName("link_video")]
        public string LinkVideo { get; set; }

        [JsonPropertyName("usuario_id")]
        [Required]
        public string UsuarioId { get; set; }
    }
}
