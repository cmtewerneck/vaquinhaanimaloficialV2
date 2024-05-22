using VaquinhaAnimal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VaquinhaAnimal.Data.Mappings
{
    public class CartaoMapping : IEntityTypeConfiguration<Cartao>
    {
        public void Configure(EntityTypeBuilder<Cartao> builder)
        {
            builder.Property(p => p.Card_Id)
                .IsRequired();

            builder.Property(p => p.Customer_Id)
                .IsRequired();

            builder.Property(p => p.Exp_Month)
                .IsRequired();

            builder.Property(p => p.Exp_Year)
                .IsRequired();

            builder.Property(p => p.First_Six_Digits)
                .IsRequired();

            builder.Property(p => p.Last_Four_Digits)
                .IsRequired();

            builder.Property(p => p.Status)
                .IsRequired();

            builder.ToTable("Cartoes");
        }
    }
}
