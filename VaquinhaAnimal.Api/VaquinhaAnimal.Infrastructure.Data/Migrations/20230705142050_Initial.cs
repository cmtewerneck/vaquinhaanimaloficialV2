using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace VaquinhaAnimal.Infrastructure.Data.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Adocoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomePet = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Celular = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    TipoPet = table.Column<int>(type: "int", nullable: false),
                    FaixaEtaria = table.Column<int>(type: "int", nullable: false),
                    UrlAdocao = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Castrado = table.Column<bool>(type: "bit", nullable: false),
                    Descricao = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    TipoAnunciante = table.Column<int>(type: "int", nullable: false),
                    Abrigo_Nome = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    Empresa_Nome = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    Particular_Nome = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    Adotado = table.Column<bool>(type: "bit", nullable: false),
                    Foto = table.Column<string>(type: "varchar(100)", nullable: true),
                    LinkVideo = table.Column<string>(type: "varchar(100)", nullable: true),
                    UsuarioId = table.Column<string>(type: "varchar(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adocoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Artigos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Titulo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Resumo = table.Column<string>(type: "varchar(1500)", maxLength: 1500, nullable: false),
                    EscritoPor = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Html = table.Column<string>(type: "varchar(8000)", maxLength: 8000, nullable: false),
                    FotoCapa = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    UrlArtigo = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artigos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Campanhas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TipoCampanha = table.Column<int>(type: "int", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TagCampanha = table.Column<int>(type: "int", nullable: false),
                    DuracaoDias = table.Column<int>(type: "int", nullable: true),
                    DataEncerramento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Titulo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    VideoUrl = table.Column<string>(type: "varchar(100)", nullable: true),
                    DescricaoCurta = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    UrlCampanha = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    DescricaoLonga = table.Column<string>(type: "varchar(5000)", maxLength: 5000, nullable: false),
                    ValorDesejado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalArrecadado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Termos = table.Column<bool>(type: "bit", nullable: false),
                    Premium = table.Column<bool>(type: "bit", nullable: false),
                    StatusCampanha = table.Column<int>(type: "int", nullable: false),
                    Usuario_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campanhas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cartoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Card_Id = table.Column<string>(type: "varchar(100)", nullable: false),
                    Customer_Id = table.Column<string>(type: "varchar(100)", nullable: false),
                    First_Six_Digits = table.Column<string>(type: "varchar(100)", nullable: false),
                    Last_Four_Digits = table.Column<string>(type: "varchar(100)", nullable: false),
                    Exp_Month = table.Column<int>(type: "int", nullable: false),
                    Exp_Year = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cartoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suportes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Usuario_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Assunto = table.Column<string>(type: "varchar(100)", nullable: false),
                    Mensagem = table.Column<string>(type: "varchar(100)", nullable: false),
                    Resposta = table.Column<string>(type: "varchar(100)", nullable: true),
                    Respondido = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suportes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Assinatura",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionId = table.Column<string>(type: "varchar(100)", nullable: false),
                    CampanhaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assinatura", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assinatura_Campanhas_CampanhaId",
                        column: x => x.CampanhaId,
                        principalTable: "Campanhas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Beneficiario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "varchar(100)", nullable: false),
                    Documento = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    Tipo = table.Column<string>(type: "varchar(100)", nullable: false),
                    CodigoBanco = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    NumeroAgencia = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: false),
                    DigitoAgencia = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: true),
                    NumeroConta = table.Column<string>(type: "varchar(13)", maxLength: 13, nullable: false),
                    DigitoConta = table.Column<string>(type: "varchar(2)", maxLength: 2, nullable: false),
                    TipoConta = table.Column<string>(type: "varchar(100)", nullable: false),
                    RecebedorId = table.Column<string>(type: "varchar(100)", nullable: true),
                    Campanha_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beneficiario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Beneficiario_Campanhas_Campanha_Id",
                        column: x => x.Campanha_Id,
                        principalTable: "Campanhas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorPlataforma = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorDestinadoPlataforma = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorBeneficiario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorTaxa = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FormaPagamento = table.Column<string>(type: "varchar(100)", nullable: false),
                    Status = table.Column<string>(type: "varchar(100)", nullable: false),
                    Transacao_Id = table.Column<string>(type: "varchar(100)", nullable: false),
                    Url_Download = table.Column<string>(type: "varchar(100)", nullable: true),
                    Charge_Id = table.Column<string>(type: "varchar(100)", nullable: false),
                    Customer_Id = table.Column<string>(type: "varchar(100)", nullable: false),
                    Usuario_Id = table.Column<string>(type: "varchar(100)", nullable: false),
                    Campanha_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doacoes_Campanhas_Campanha_Id",
                        column: x => x.Campanha_Id,
                        principalTable: "Campanhas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Imagens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Arquivo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Campanha_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Imagens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Imagens_Campanhas_Campanha_Id",
                        column: x => x.Campanha_Id,
                        principalTable: "Campanhas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assinatura_CampanhaId",
                table: "Assinatura",
                column: "CampanhaId");

            migrationBuilder.CreateIndex(
                name: "IX_Beneficiario_Campanha_Id",
                table: "Beneficiario",
                column: "Campanha_Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Doacoes_Campanha_Id",
                table: "Doacoes",
                column: "Campanha_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Imagens_Campanha_Id",
                table: "Imagens",
                column: "Campanha_Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Adocoes");

            migrationBuilder.DropTable(
                name: "Artigos");

            migrationBuilder.DropTable(
                name: "Assinatura");

            migrationBuilder.DropTable(
                name: "Beneficiario");

            migrationBuilder.DropTable(
                name: "Cartoes");

            migrationBuilder.DropTable(
                name: "Doacoes");

            migrationBuilder.DropTable(
                name: "Imagens");

            migrationBuilder.DropTable(
                name: "Suportes");

            migrationBuilder.DropTable(
                name: "Campanhas");
        }
    }
}
