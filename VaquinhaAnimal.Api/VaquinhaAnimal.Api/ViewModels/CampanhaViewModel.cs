using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using VaquinhaAnimal.Domain.Enums;

namespace VaquinhaAnimal.Api.ViewModels
{
    public class CampanhaViewModel
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("data_criacao")]
        public DateTime DataCriacao { get; set; }

        [JsonPropertyName("tipo_campanha")]
        public TipoCampanhaEnum TipoCampanha { get; set; }

        [JsonPropertyName("data_inicio")]
        public DateTime? DataInicio { get; set; }

        [JsonPropertyName("tag_campanha")]
        public TagCampanhaEnum TagCampanha { get; set; }

        [JsonPropertyName("duracao_dias")]
        public int? DuracaoDias { get; set; }

        [JsonPropertyName("data_encerramento")]
        public DateTime? DataEncerramento { get; set; }

        [JsonPropertyName("titulo")]
        public string Titulo { get; set; }

        [JsonPropertyName("url_campanha")]
        public string UrlCampanha { get; set; }

        [JsonPropertyName("video_url")]
        public string VideoUrl { get; set; }

        [JsonPropertyName("descricao_curta")]
        public string DescricaoCurta { get; set; } // (200)

        [JsonPropertyName("descricao_longa")]
        public string DescricaoLonga { get; set; }// (1000)

        [JsonPropertyName("valor_desejado")]
        public decimal ValorDesejado { get; set; } // MAIOR QUE ZERO

        [JsonPropertyName("total_arrecadado")]
        public decimal TotalArrecadado { get; set; } // MAIOR OU IGUAL A ZERO

        [JsonPropertyName("termos")]
        public bool Termos { get; set; }

        [JsonPropertyName("premium")]
        public bool Premium { get; set; }

        [JsonPropertyName("status_campanha")]
        public int StatusCampanha { get; set; }

        [JsonPropertyName("usuario_id")]
        public Guid Usuario_Id { get; set; }

        [JsonPropertyName("imagens")]
        public List<ImagemViewModel> Imagens { get; set; }

        [JsonPropertyName("beneficiario")]
        public BeneficiarioViewModel Beneficiario { get; set; }
    }

    public class BeneficiarioViewModel
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("nome")]
        public string Nome { get; set; }

        [JsonPropertyName("documento")]
        public string Documento { get; set; }

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; }

        [JsonPropertyName("codigo_banco")]
        public string CodigoBanco { get; set; }

        [JsonPropertyName("numero_agencia")]
        public string NumeroAgencia { get; set; }

        [JsonPropertyName("digito_agencia")]
        public string DigitoAgencia { get; set; }

        [JsonPropertyName("numero_conta")]
        public string NumeroConta { get; set; }

        [JsonPropertyName("digito_conta")]
        public string DigitoConta { get; set; }

        [JsonPropertyName("tipo_conta")]
        public string TipoConta { get; set; }

        [JsonPropertyName("recebedor_id")]
        public string RecebedorId { get; set; }

        [JsonPropertyName("campanha_id")]
        public Guid Campanha_Id { get; set; }
    }
}
