using VaquinhaAnimal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VaquinhaAnimal.Data.Context
{
    public class VaquinhaDbContext : DbContext
    {
        public VaquinhaDbContext(DbContextOptions<VaquinhaDbContext> options) : base(options) { }

        #region ENTITIES CONTEXT
        public DbSet<Adocao> Adocoes { get; set; }
        public DbSet<Artigo> Artigos { get; set; }
        public DbSet<Campanha> Campanhas { get; set; }
        public DbSet<Cartao> Cartoes { get; set; }
        public DbSet<Assinatura> Assinaturas { get; set; }
        public DbSet<Beneficiario> Beneficiario { get; set; }
        public DbSet<Suporte> Suportes { get; set; }
        public DbSet<Doacao> Doacoes { get; set; }
        public DbSet<Imagem> Imagens { get; set; }
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(e => e.GetProperties()
                    .Where(p => p.ClrType == typeof(string))))
                property.SetColumnType("varchar(100)");

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(VaquinhaDbContext).Assembly);

            modelBuilder.Entity<Beneficiario>()
                .HasOne(a => a.Campanha)
                .WithOne(b => b.Beneficiario)
                .HasForeignKey<Beneficiario>(b => b.Campanha_Id);

            base.OnModelCreating(modelBuilder);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var entry in ChangeTracker.Entries().Where(entry => entry.Entity.GetType().GetProperty("DataCadastro") != null))
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Property("DataCadastro").CurrentValue = DateTime.Now;
                }

                if (entry.State == EntityState.Modified)
                {
                    entry.Property("DataCadastro").IsModified = false;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
