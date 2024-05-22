using VaquinhaAnimal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VaquinhaAnimal.Data.Mappings
{
    public class AssinaturaMapping : IEntityTypeConfiguration<Assinatura>
    {
        public void Configure(EntityTypeBuilder<Assinatura> builder)
        {
            builder.Property(p => p.CampanhaId)
                .IsRequired();

            builder.Property(p => p.SubscriptionId)
                .IsRequired();

            // RELATIONSHIP
            builder.HasOne(x => x.Campanha)
                .WithMany(x => x.Assinaturas)
                .HasForeignKey(x => x.CampanhaId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.ToTable("Assinatura");
        }
    }
}
