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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Empleado>(e =>
            {
                // AspNetUsers.Id suele ser nvarchar(450)
                e.Property(x => x.UserId).HasMaxLength(450);

                // FK opcional Empleado.UserId -> AspNetUsers.Id
                e.HasOne(x => x.User)
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.SetNull); // si borras el usuario, queda desvinculado

                // Un empleado no puede apuntar a 2 usuarios y permite muchos NULL
                e.HasIndex(x => x.UserId)
                 .IsUnique()
                 .HasFilter("[UserId] IS NOT NULL");
            });
        }
    }
}
