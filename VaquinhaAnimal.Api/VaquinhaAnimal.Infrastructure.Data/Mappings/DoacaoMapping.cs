using VaquinhaAnimal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VaquinhaAnimal.Data.Mappings
{
    public class DoacaoMapping : IEntityTypeConfiguration<Doacao>
    {
        public void Configure(EntityTypeBuilder<Doacao> builder)
        {
            builder.Property(p => p.FormaPagamento)
                .IsRequired();

            builder.Property(p => p.Status)
                .IsRequired();

            builder.Property(p => p.Transacao_Id)
                .IsRequired();

            builder.Property(p => p.Customer_Id)
                .IsRequired();

            builder.Property(p => p.Charge_Id)
                .IsRequired();

            builder.Property(p => p.Usuario_Id)
                .IsRequired();

            // RELATIONSHIP
            builder.HasOne(x => x.Campanha)
                .WithMany(x => x.Doacoes)
                .HasForeignKey(x => x.Campanha_Id)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.ToTable("Doacoes");
        }
    }
}
