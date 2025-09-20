using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkyNet.Models;

namespace SkyNet.Data
{
    // Puedes dejarlo sin genéricos o ser explícito con <IdentityUser>
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Cliente> Clientes => Set<Cliente>();
        public DbSet<Solicitud> Solicitudes => Set<Solicitud>();
        public DbSet<Empleado> Empleados => Set<Empleado>();
        public DbSet<ArchivoSolicitud> ArchivosSolicitudes => Set<ArchivoSolicitud>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // ===== Empleado =====
            b.Entity<Empleado>(e =>
            {
                // AspNetUsers.Id suele ser nvarchar(450)
                e.Property(x => x.UserId).HasMaxLength(450);

                // FK opcional Empleado.UserId -> AspNetUsers.Id
                e.HasOne(x => x.User)
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.SetNull); // si borras el usuario, queda desvinculado

                // Un Empleado apunta a lo sumo a 1 usuario; permite muchos NULL
                e.HasIndex(x => x.UserId)
                 .IsUnique()
                 .HasFilter("[UserId] IS NOT NULL");
            });

            // ===== Solicitud =====
            b.Entity<Solicitud>(e =>
            {
                e.ToTable("Solicitudes");
                e.HasKey(x => x.Id);

                e.HasMany(x => x.Archivos)
                 .WithOne(a => a.Solicitud)
                 .HasForeignKey(a => a.Fk_Solicitud)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== ArchivoSolicitud =====
            b.Entity<ArchivoSolicitud>(e =>
            {
                e.ToTable("Archivos_solicitudes");
                e.HasKey(x => x.Id);

                // Forzamos el nombre de la FK en la tabla
                e.Property(x => x.Fk_Solicitud)
                 .HasColumnName("fk_solicitud")   // << aquí se renombra
                 .IsRequired();

                e.Property(x => x.PublicId)
                 .HasColumnName("public_id")
                 .HasMaxLength(512)
                 .IsRequired();

                e.Property(x => x.CreatedAtUtc)
                 .HasColumnName("created_at_utc")
                 .IsRequired();

                e.Property(x => x.Estado)
                .HasColumnName("estado");
              

                // Relación con solicitudes
                e.HasOne(x => x.Solicitud)
                 .WithMany(s => s.Archivos)
                 .HasForeignKey(x => x.Fk_Solicitud)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
