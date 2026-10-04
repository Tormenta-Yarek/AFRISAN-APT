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

        // NUEVO: catálogo de Órdenes de Trabajo
        public DbSet<OrdenTrabajo> OrdenesTrabajo
        {
            get;
            set;
        }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================================================
            // CILINDROS
            // =====================================================

            // Impedir códigos QR exactamente duplicados.
            modelBuilder.Entity<Cilindro>()
                .HasIndex(c => c.CodigoQr)
                .IsUnique();

            // =====================================================
            // ÓRDENES DE TRABAJO
            // =====================================================

            // El código de la OT no puede repetirse.
            modelBuilder.Entity<OrdenTrabajo>()
                .HasIndex(o => o.Codigo)
                .IsUnique();

            // =====================================================
            // MOVIMIENTOS DE INVENTARIO
            // =====================================================

            // Conservar la relación entre movimientos y cilindros.
            modelBuilder.Entity<MovimientoInventario>()
                .HasOne(m => m.Cilindro)
                .WithMany()
                .HasForeignKey(m => m.CilindroId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación movimiento -> Orden de Trabajo.
            modelBuilder.Entity<MovimientoInventario>()
                .HasOne(m => m.OrdenTrabajo)
                .WithMany(o => o.Movimientos)
                .HasForeignKey(m => m.OrdenTrabajoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}