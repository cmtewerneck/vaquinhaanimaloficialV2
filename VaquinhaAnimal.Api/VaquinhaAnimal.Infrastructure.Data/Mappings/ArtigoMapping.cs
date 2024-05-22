using VaquinhaAnimal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VaquinhaAnimal.Data.Mappings
{
    public class ArtigoMapping : IEntityTypeConfiguration<Artigo>
    {
        public void Configure(EntityTypeBuilder<Artigo> builder)
        {
            builder.Property(p => p.Titulo)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("varchar(500)");

            builder.Property(p => p.Resumo)
                .IsRequired()
                .HasMaxLength(1500)
                .HasColumnType("varchar(1500)");

            builder.Property(p => p.EscritoPor)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("varchar(50)");

            builder.Property(p => p.UrlArtigo)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("varchar(200)");

            builder.Property(p => p.Html)
                .IsRequired()
                .HasMaxLength(10000)
                .HasColumnType("varchar(10000)");

            builder.Property(p => p.FotoCapa)
                .HasMaxLength(500)
                .HasColumnType("varchar(500)");

            builder.ToTable("Artigos");
        }
    }
}
