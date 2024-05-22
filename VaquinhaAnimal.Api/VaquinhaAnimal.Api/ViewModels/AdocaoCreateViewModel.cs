using System;
using System.Text.Json.Serialization;
using VaquinhaAnimal.Domain.Enums;

namespace VaquinhaAnimal.Api.ViewModels
{
    public class AdocaoCreateViewModel
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("nome_pet")]
        public string NomePet { get; set; }

        [JsonPropertyName("tipo_anunciante")]
        public TipoAnuncianteEnum TipoAnunciante { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("url_adocao")]
        public string UrlAdocao { get; set; }

        [JsonPropertyName("descricao")]
        public string Descricao { get; set; }

        [JsonPropertyName("faixa_etaria")]
        public FaixaEtariaEnum FaixaEtaria { get; set; }

        [JsonPropertyName("link_video")]
        public string LinkVideo { get; set; }

        [JsonPropertyName("celular")]
        public string Celular { get; set; }

        [JsonPropertyName("instagram")]
        public string Instagram { get; set; }

        [JsonPropertyName("facebook")]
        public string Facebook { get; set; }

        [JsonPropertyName("tipo_pet")]
        public TipoPetEnum TipoPet { get; set; }

        [JsonPropertyName("castrado")]
        public bool Castrado { get; set; }

        [JsonPropertyName("abrigo_nome")]
        public string Abrigo_Nome { get; set; }

        [JsonPropertyName("particular_nome")]
        public string Particular_Nome { get; set; }

        [JsonPropertyName("empresa_nome")]
        public string Empresa_Nome { get; set; }

        [JsonPropertyName("adotado")]
        public bool Adotado { get; set; }

        [JsonPropertyName("foto")]
        public string Foto { get; set; }

        [JsonPropertyName("foto_upload")]
        public string FotoUpload { get; set; }

        [JsonPropertyName("usuario_id")]
        public string UsuarioId { get; set; }
    }
}

