using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VaquinhaAnimal.Domain.Entities.Base
{
    public class ApplicationUser : IdentityUser
    {
        // Obrigatório --> 64 caracteres
        [JsonPropertyName("name")]
        public string Name { get; set; }

        // Obrigatório --> 64 caracteres
        [JsonPropertyName("email")]
        [MaxLength(64)]
        public override string Email { get => base.Email; set => base.Email = value; }

        // Opcional --> deixar em branco
        [JsonPropertyName("code")]
        public string Code { get; set; }

        // Obrigatório --> 10 caracteres
        [JsonPropertyName("type")]
        [MaxLength(10)]
        public string Type { get; set; }

        // Obrigatório --> 14 caracteres
        [JsonPropertyName("document")]
        [MaxLength(14)]
        public string Document { get; set; }

        // Opcional
        [JsonPropertyName("codigo_pagarme")]
        public string Codigo_Pagarme { get; set; }

        // Opcional
        [JsonPropertyName("foto")]
        public string Foto { get; set; }
    }
}
