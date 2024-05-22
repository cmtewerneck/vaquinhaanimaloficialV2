using VaquinhaAnimal.Domain.Entities.Base;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VaquinhaAnimal.Domain.Entities
{
    public class Doacao : BaseEntity
    {
        [JsonPropertyName("data")]
        [Required]
        public DateTime Data { get; set; }

        [JsonPropertyName("valor")]
        [Required]
        public decimal Valor { get; set; }

        [JsonPropertyName("valor_plataforma")]
        public decimal ValorPlataforma { get; set; }

        [JsonPropertyName("valor_destinado_plataforma")]
        public decimal ValorDestinadoPlataforma { get; set; }

        [JsonPropertyName("valor_beneficiario")]
        public decimal ValorBeneficiario { get; set; }

        [JsonPropertyName("valor_taxa")]
        public decimal ValorTaxa { get; set; }

        [JsonPropertyName("forma_pagamento")]
        [Required]
        public string FormaPagamento { get; set; }

        [JsonPropertyName("status")]
        [Required]
        public string Status { get; set; }

        [JsonPropertyName("transacao_id")]
        [Required]
        public string Transacao_Id { get; set; }

        [JsonPropertyName("url_download")]
        public string Url_Download { get; set; }

        [JsonPropertyName("charge_id")]
        [Required]
        public string Charge_Id { get; set; }

        [JsonPropertyName("customer_id")]
        [Required]
        public string Customer_Id { get; set; }

        [JsonPropertyName("usuario_id")]
        [Required]
        public string Usuario_Id { get; set; }

        public Campanha Campanha { get; set; }

        [JsonPropertyName("campanha_id")]
        [Required]
        public Guid Campanha_Id { get; set; } 
    }
}
