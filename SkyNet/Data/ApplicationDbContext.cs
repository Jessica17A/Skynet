using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkyNet.Models;
using SkyNet.Models.DTOs;
using System.Reflection.Emit;

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

        public DbSet<GrupoSupervisorTec> GruposSupervisoresTec => Set<GrupoSupervisorTec>();

        public DbSet<SolicitudAsignacion> SolicitudAsignaciones { get; set; } = null!;

        public DbSet<SolicitudAsignacionListado> SolicitudAsignacionListado { get; set; } = default!;
        public DbSet<SolicitudAsignacionListado> SolicitudAsignacionListadoS { get; set; } = default!;

        public DbSet<SolicitudResumenDto> SolicitudResumen { get; set; } = null!;

        public DbSet<SolicitudAsignacionDetalleDto> SolicitudAsignacionDetalle => Set<SolicitudAsignacionDetalleDto>();        

        public DbSet<SolicitudTracking> SolicitudTrackings { get; set; } = null!;

        public DbSet<SolicitudTrackingTimelineRow> SolicitudTrackingTimeline { get; set; } = null!;

        public DbSet<SolicitudDetalleCompletoDto> SolicitudDetalleCompleto { get; set; }




        protected override void OnModelCreating(ModelBuilder b)
        {           
            base.OnModelCreating(b);

            b.Entity<SolicitudAsignacionDetalleDto>(e =>
            {
                e.HasNoKey();
                e.ToView(null); 
            });

            b.Entity<SolicitudResumenDto>(e =>
            {
                e.HasNoKey();
                e.ToView(null);
                e.Property(p => p.IdSolicitud).HasColumnName("IDSOLICITUD");
                e.Property(p => p.FechaVisita_Min).HasColumnName("FECHAVISITA_MIN");
                e.Property(p => p.Estado_Agregado).HasColumnName("ESTADO_AGREGADO");
                e.Property(p => p.Supervisores).HasColumnName("SUPERVISORES");
                e.Property(p => p.Tecnicos).HasColumnName("TECNICOS");
                e.Property(p => p.Asignaciones_Json).HasColumnName("ASIGNACIONES_JSON");
            });

            b.Entity<SolicitudAsignacionListado>().HasNoKey().ToView(null);
            b.Entity<SolicitudAsignacionListadoS>().HasNoKey().ToView(null);

            // ===== Empleado =====
            b.Entity<Empleado>(e =>
            {
                e.ToTable("Empleado");

                // PK con nombre explícito (opcional, pero recomendado)
                e.HasKey(x => x.Id).HasName("Id_Empleado");

                // Columnas (solo si quieres fijar explícitamente los nombres)
                e.Property(x => x.Id).HasColumnName("Id_Empleado");
                e.Property(x => x.UserId).HasColumnName("UserId").HasMaxLength(450);

                // FK a AspNetUsers con nombre de constraint explícito
                e.HasOne(x => x.User)
                 .WithMany() // no hay colección inversa en IdentityUser
                 .HasForeignKey(x => x.UserId)
                 .HasConstraintName("FK_Empleado_AspNetUsers_UserId")
                 .OnDelete(DeleteBehavior.SetNull);

                // Índice único para asegurar 1 Empleado por usuario (si UserId no es null)
                e.HasIndex(x => x.UserId)
                 .IsUnique()
                 .HasFilter("[UserId] IS NOT NULL")
                 .HasDatabaseName("IX_Empleado_UserId_Unico");
            });



            // ===== Solicitud =====
            b.Entity<Solicitud>(e =>
            {
                e.ToTable("Solicitud"); // singular
                e.HasKey(x => x.Id).HasName("Id_Solicitud");
                e.Property(x => x.Id).HasColumnName("Id_Solicitud");

                e.HasMany(x => x.Archivos)
                 .WithOne(a => a.Solicitud)
                 .HasForeignKey(a => a.Fk_Solicitud)
                 .OnDelete(DeleteBehavior.Cascade);
            });



            // ===== GrupoSupervisorTec =====
            b.Entity<GrupoSupervisorTec>(e =>
            {
                e.ToTable("Grupo_Supervisor_Tec");                // 👈 singular
                e.HasKey(x => x.IdGrupo).HasName("Id_Grupo_Supervisor_Tec");

                e.Property(x => x.IdGrupo).HasColumnName("IdGrupo");
                e.Property(x => x.FkSupervisor).HasColumnName("FkSupervisor");
                e.Property(x => x.FkTecnico).HasColumnName("FkTecnico");
                e.Property(x => x.FechaCreacionUtc).HasColumnName("FechaCreacionUtc");
                e.Property(x => x.Estado)
                 .HasColumnType("bit")
                 .HasColumnName("Estado")
                 .ValueGeneratedNever();

                e.HasOne(x => x.Supervisor)
                 .WithMany()
                 .HasForeignKey(x => x.FkSupervisor)
                 .HasConstraintName("FK_GrupoSupervisor_Supervisor")
                 .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.Tecnico)
                 .WithMany()
                 .HasForeignKey(x => x.FkTecnico)
                 .HasConstraintName("FK_GrupoSupervisor_Tecnico")
                 .OnDelete(DeleteBehavior.NoAction);
            });



            b.Entity<SolicitudAsignacion>(e =>
            {
                e.ToTable("Solicitud_Asignacion"); // singular
                e.HasKey(x => x.Id).HasName("Id_Solicitud_Asignacion");
                e.Property(x => x.Id).HasColumnName("Id_Solicitud_Asignacion");

                e.Property(x => x.FkSolicitud).HasColumnName("FkSolicitud");
                e.Property(x => x.IdGrupo).HasColumnName("IdGrupo");
                e.Property(x => x.FkTecnico).HasColumnName("FkTecnico");
                e.Property(x => x.FechaAsignacionUtc).HasColumnName("FechaAsignacionUtc");
                e.Property(x => x.Fecha_Inicio).HasColumnName("FechaInicio").HasColumnType("datetime2").IsRequired(false);
                e.Property(x => x.Fecha_Fin).HasColumnName("FechaFin").HasColumnType("datetime2").IsRequired(false);
                e.Property(x => x.Notas).HasColumnName("Notas").HasMaxLength(500);
                e.Property(x => x.Estado).HasConversion<byte>().HasColumnName("Estado");

                e.HasIndex(x => new { x.FkSolicitud, x.FkTecnico, x.Estado })
                 .HasDatabaseName("UX_Solicitud_Asignacion_ActivaPorTec")
                 .HasFilter("[Estado] = 1")
                 .IsUnique();
            });


            b.Entity<SolicitudTracking>(e =>
            {
                e.ToTable("Solicitud_Tracking", "dbo");

                // PK
                e.HasKey(x => x.IdTracking)
                 .HasName("PK_Solicitud_Tracking");

                e.Property(x => x.IdTracking)
                 .HasColumnName("IdTracking");                  // BIGINT IDENTITY(1,1)

                e.Property(x => x.FkSolicitud)
                 .HasColumnName("FkSolicitud");                 // BIGINT NOT NULL

                e.Property(x => x.UserId)
                 .HasColumnName("UserId")
                 .HasMaxLength(450)
                 .IsRequired();

                // tinyint NULL
                e.Property(x => x.Estado)
                 .HasColumnName("Estado"); // byte? ya mapea a tinyint

                // datetime2(7) con default SYSUTCDATETIME()
                e.Property(x => x.FechaUtc)
                 .HasColumnName("FechaUtc")
                 .HasColumnType("datetime2(7)")
                 .HasDefaultValueSql("SYSUTCDATETIME()");

                e.HasOne(x => x.Solicitud)
                 .WithMany() // sin navegación inversa
                 .HasForeignKey(x => x.FkSolicitud)
                 .HasConstraintName("FK_SolicitudTracking_Solicitud");


            });

            b.Entity<SolicitudTrackingTimelineRow>(e =>
            {
                e.HasNoKey();      // <- importantísimo
                e.ToView(null);    // <- evita que EF lo trate como tabla o vista
                e.Property(p => p.SolicitudId).HasColumnName("SolicitudId");
                e.Property(p => p.FechaUtc).HasColumnName("FechaUtc");
                e.Property(p => p.Usuario).HasColumnName("Usuario");
                e.Property(p => p.Texto).HasColumnName("Texto");
                e.Property(p => p.Estado).HasColumnName("Estado");
                e.Property(p => p.EstadoTexto).HasColumnName("EstadoTexto");
            });






        }

    }
}
