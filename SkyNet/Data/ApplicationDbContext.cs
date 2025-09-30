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

        public DbSet<GrupoSupervisorTec> GruposSupervisoresTec => Set<GrupoSupervisorTec>();

        public DbSet<SolicitudAsignacion> SolicitudAsignaciones { get; set; } = null!;

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
                 .OnDelete(DeleteBehavior.SetNull);

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

                e.Property(x => x.Fk_Solicitud)
                 .HasColumnName("fk_solicitud")
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

                e.HasOne(x => x.Solicitud)
                 .WithMany(s => s.Archivos)
                 .HasForeignKey(x => x.Fk_Solicitud)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== GrupoSupervisorTec =====
            b.Entity<GrupoSupervisorTec>(e =>
            {
                e.ToTable("Grupos_Supervisores_Tec");
                e.HasKey(x => x.IdGrupo);

                e.Property(x => x.IdGrupo).HasColumnName("IDGRUPO");
                e.Property(x => x.FkSupervisor).HasColumnName("FKSUPERVISOR");
                e.Property(x => x.FkTecnico).HasColumnName("FKTECNICO");
                e.Property(x => x.FechaCreacionUtc).HasColumnName("FECHA_CREACION_UTC");
                e.Property(x => x.Estado)
                 .HasColumnType("bit")
                 .HasColumnName("ESTADO")
                 .ValueGeneratedNever();

                e.HasOne(x => x.Supervisor)
                 .WithMany()
                 .HasForeignKey(x => x.FkSupervisor)
                 .HasConstraintName("FK_GRUPO_SUPERVISOR")
                 .OnDelete(DeleteBehavior.NoAction);   // 👈 sin cascada

                e.HasOne(x => x.Tecnico)
                 .WithMany()
                 .HasForeignKey(x => x.FkTecnico)
                 .HasConstraintName("FK_GRUPO_TECNICO")
                 .OnDelete(DeleteBehavior.NoAction);   // 👈 sin cascada
            });


            b.Entity<SolicitudAsignacion>(e =>
            {
                e.ToTable("Solicitudes_Asignaciones");

                e.HasKey(x => x.Id);

                e.Property(x => x.FkSolicitud)
                 .IsRequired()
                 .HasColumnName("FKSOLICITUD");

                e.Property(x => x.IdGrupo)
                 .IsRequired()
                 .HasColumnName("IDGRUPO");

                e.Property(x => x.FkTecnico)
                 .IsRequired()
                 .HasColumnName("FKTECNICO");

                e.Property(x => x.FechaAsignacionUtc)
                 .HasColumnName("FECHA_ASIGNACION_UTC");
                e.Property(x => x.Fecha_Inicio).HasColumnName("Fecha_Inicio").HasColumnType("datetime2").IsRequired(false);
                e.Property(x => x.Fecha_Fin).HasColumnName("Fecha_Fin").HasColumnType("datetime2").IsRequired(false);

                e.Property(x => x.Notas)
                 .HasMaxLength(500)
                 .HasColumnName("NOTAS");

                e.Property(x => x.Estado)
                 .HasConversion<byte>()
                 .HasColumnName("ESTADO");

                e.HasIndex(x => new { x.FkSolicitud, x.FkTecnico, x.Estado })
                 .HasDatabaseName("UX_Sol_Asig_ActivaPorTec")
                 .HasFilter("[Estado] = 1")
                 .IsUnique();


            });

        }

    }
}
