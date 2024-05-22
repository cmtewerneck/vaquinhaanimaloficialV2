using VaquinhaAnimal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VaquinhaAnimal.Data.Mappings
{
    public class ImagemMapping : IEntityTypeConfiguration<Imagem>
    {
        public void Configure(EntityTypeBuilder<Imagem> builder)
        {
            builder.Property(p => p.Tipo)
                .IsRequired();

            builder.Property(p => p.Arquivo)
                .IsRequired()
                .HasMaxLength(500);

            // RELATIONSHIP
            builder.HasOne(x => x.Campanha)
                .WithMany(x => x.Imagens)
                .HasForeignKey(x => x.Campanha_Id)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.ToTable("Imagens");
        }
    }
}
