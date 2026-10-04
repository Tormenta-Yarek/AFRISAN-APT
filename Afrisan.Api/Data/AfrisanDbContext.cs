using Afrisan.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Afrisan.Api.Data
{
    public class AfrisanDbContext : DbContext
    {
        public AfrisanDbContext(
            DbContextOptions<AfrisanDbContext> options)
            : base(options)
        {
        }

        public DbSet<Cilindro> Cilindros { get; set; }

        public DbSet<GasRefrigerante> GasesRefrigerantes { get; set; }

        public DbSet<MovimientoInventario> MovimientosInventario
        {
            get;
            set;
        }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Impedir códigos QR exactamente duplicados.
            modelBuilder.Entity<Cilindro>()
                .HasIndex(c => c.CodigoQr)
                .IsUnique();

            // Conservar la relación entre movimientos y cilindros.
            modelBuilder.Entity<MovimientoInventario>()
                .HasOne(m => m.Cilindro)
                .WithMany()
                .HasForeignKey(m => m.CilindroId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}