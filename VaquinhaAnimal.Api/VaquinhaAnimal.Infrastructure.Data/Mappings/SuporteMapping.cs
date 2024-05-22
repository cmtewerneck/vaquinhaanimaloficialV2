using VaquinhaAnimal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VaquinhaAnimal.Data.Mappings
{
    public class SuporteMapping : IEntityTypeConfiguration<Suporte>
    {
        public void Configure(EntityTypeBuilder<Suporte> builder)
        {
            builder.Property(p => p.Data)
                .IsRequired();

            builder.Property(p => p.Usuario_Id)
                .IsRequired();

            builder.Property(p => p.Assunto)
                .IsRequired();

            builder.Property(p => p.Mensagem)
                .IsRequired();

            builder.Property(p => p.Respondido)
                .IsRequired();

            builder.ToTable("Suportes");
        }
    }
}
