using Microsoft.EntityFrameworkCore.Migrations;

namespace VaquinhaAnimal.Infrastructure.Data.Migrations
{
    public partial class AlterandoAdocao : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Celular",
                table: "Adocoes",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "Facebook",
                table: "Adocoes",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Instagram",
                table: "Adocoes",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Facebook",
                table: "Adocoes");

            migrationBuilder.DropColumn(
                name: "Instagram",
                table: "Adocoes");

            migrationBuilder.AlterColumn<string>(
                name: "Celular",
                table: "Adocoes",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }
    }
}
