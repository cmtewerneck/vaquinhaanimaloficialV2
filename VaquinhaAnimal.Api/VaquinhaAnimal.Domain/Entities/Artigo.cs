using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using VaquinhaAnimal.Domain.Entities.Base;

namespace VaquinhaAnimal.Domain.Entities
{
    public class Artigo : BaseEntity
    {
        [JsonPropertyName("titulo")]
        [Required]
        public string Titulo { get; set; }

        [JsonPropertyName("resumo")]
        [Required]
        public string Resumo { get; set; }

        [JsonPropertyName("escrito_por")]
        [Required]
        public string EscritoPor { get; set; }

        [JsonPropertyName("html")]
        [Required]
        public string Html { get; set; }

        [JsonPropertyName("foto_capa")]
        public string FotoCapa { get; set; }

        [JsonPropertyName("url_artigo")]
        [Required]
        public string UrlArtigo { get; set; } // (200)
    }
}
