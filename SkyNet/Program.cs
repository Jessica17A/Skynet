using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SkyNet.Data;

var builder = WebApplication.CreateBuilder(args);

// ------------------ Services (ANTES de Build) ------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity (NO se quita)
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// MVC (vistas) + API + HttpClient
builder.Services.AddControllersWithViews();  // Vistas
builder.Services.AddControllers();           // API (attribute routing)
builder.Services.AddHttpClient();

var app = builder.Build();

// ------------------ Pipeline ------------------
// Si te da guerra el certificado en dev, deja comentado HTTPS:
// app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // opcional si usas HTTPS
}

app.UseStaticFiles();

app.UseRouting();

// Identity: ¡autenticación ANTES de autorización!
app.UseAuthentication();
app.UseAuthorization();

// Rutas MVC convencionales
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=LayoutPrincipal}/{action=Index}/{id?}");

// Páginas de Identity (si las usas)
app.MapRazorPages();

// Rutas de API por atributo (Controllers en /Api)
app.MapControllers();

app.UseStaticFiles();


app.Run();
