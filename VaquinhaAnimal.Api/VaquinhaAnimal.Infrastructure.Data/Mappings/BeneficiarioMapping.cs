using VaquinhaAnimal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VaquinhaAnimal.Data.Mappings
{
    public class BeneficiarioMapping : IEntityTypeConfiguration<Beneficiario>
    {
        public void Configure(EntityTypeBuilder<Beneficiario> builder)
        {
            builder.Property(p => p.Nome)
                .IsRequired();

            builder.Property(p => p.Documento)
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(p => p.Tipo)
                .IsRequired();

            builder.Property(p => p.CodigoBanco)
                .HasMaxLength(3)
                .IsRequired();

            builder.Property(p => p.NumeroAgencia)
                .HasMaxLength(4)
                .IsRequired();

            builder.Property(p => p.DigitoAgencia)
                .HasMaxLength(1);

            builder.Property(p => p.NumeroConta)
                .HasMaxLength(13)
                .IsRequired();

            builder.Property(p => p.DigitoConta)
                .HasMaxLength(2)
                .IsRequired();

            builder.Property(p => p.TipoConta)
                .IsRequired();

            builder.Property(p => p.Campanha_Id)
                .IsRequired();

            builder.ToTable("Beneficiario");
        }
    }
}
