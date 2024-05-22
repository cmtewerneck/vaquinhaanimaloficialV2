using VaquinhaAnimal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VaquinhaAnimal.Data.Mappings
{
    public class AdocaoMapping : IEntityTypeConfiguration<Adocao>
    {
        public void Configure(EntityTypeBuilder<Adocao> builder)
        {
            builder.Property(p => p.Adotado)
                .IsRequired();

            builder.Property(p => p.Castrado)
                .IsRequired();

            builder.Property(p => p.TipoPet)
                .IsRequired();

            builder.Property(p => p.TipoAnunciante)
                .IsRequired();

            builder.Property(p => p.FaixaEtaria)
                .IsRequired();

            builder.Property(p => p.UrlAdocao)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("varchar(200)");

            builder.Property(p => p.UsuarioId)
                .IsRequired();

            builder.Property(p => p.NomePet)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("varchar(50)");

            builder.Property(p => p.Descricao)
                .HasMaxLength(1000)
                .HasColumnType("varchar(1000)");

            builder.Property(p => p.Celular)
                .HasMaxLength(20)
                .HasColumnType("varchar(20)");

            builder.Property(p => p.Instagram)
                .HasMaxLength(200)
                .HasColumnType("varchar(200)");

            builder.Property(p => p.Facebook)
                .HasMaxLength(200)
                .HasColumnType("varchar(200)");

            builder.Property(p => p.Email)
                .HasMaxLength(100)
                .HasColumnType("varchar(100)");

            builder.Property(p => p.Abrigo_Nome)
                .HasMaxLength(50)
                .HasColumnType("varchar(50)");

            builder.Property(p => p.Empresa_Nome)
                .HasMaxLength(50)
                .HasColumnType("varchar(50)");

            builder.Property(p => p.Particular_Nome)
                .HasMaxLength(50)
                .HasColumnType("varchar(50)");

            builder.ToTable("Adocoes");
        }
    }
}
