using Afrisan.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Afrisan.Api.Data
{
    public class AfrisanDbContext : IdentityDbContext<Usuario>
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

        public DbSet<OrdenTrabajo> OrdenesTrabajo
        {
            get;
            set;
        }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            // IMPORTANTE:
            // Configura primero las tablas internas de ASP.NET Identity.
            base.OnModelCreating(modelBuilder);

            // =====================================================
            // CILINDROS
            // =====================================================

            modelBuilder.Entity<Cilindro>()
                .HasIndex(c => c.CodigoQr)
                .IsUnique();

            // =====================================================
            // ÓRDENES DE TRABAJO
            // =====================================================

            modelBuilder.Entity<OrdenTrabajo>()
                .HasIndex(o => o.Codigo)
                .IsUnique();

            // =====================================================
            // MOVIMIENTOS DE INVENTARIO
            // =====================================================

            modelBuilder.Entity<MovimientoInventario>()
                .HasOne(m => m.Cilindro)
                .WithMany()
                .HasForeignKey(m => m.CilindroId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MovimientoInventario>()
                .HasOne(m => m.OrdenTrabajo)
                .WithMany(o => o.Movimientos)
                .HasForeignKey(m => m.OrdenTrabajoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}