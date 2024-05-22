using VaquinhaAnimal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VaquinhaAnimal.Data.Mappings
{
    public class CampanhaMapping : IEntityTypeConfiguration<Campanha>
    {
        public void Configure(EntityTypeBuilder<Campanha> builder)
        {
            builder.Property(p => p.Titulo)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("varchar(100)");

            builder.Property(p => p.DescricaoCurta)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("varchar(200)");

            builder.Property(p => p.DescricaoLonga)
                .IsRequired()
                .HasMaxLength(5000)
                .HasColumnType("varchar(5000)");

            builder.Property(p => p.UrlCampanha)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("varchar(200)");

            builder.Property(p => p.Usuario_Id)
               .IsRequired();

            // RELATIONSHIP
            builder.HasOne(f => f.Beneficiario)
               .WithOne();

            builder.ToTable("Campanhas");
        }
    }
}
