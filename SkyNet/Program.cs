using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CloudinaryDotNet;
using SkyNet.Data;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;   // en dev puedes poner false si te estorba
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// MVC + HttpClient
builder.Services.AddControllersWithViews();   // basta para MVC + API con attribute routing
builder.Services.AddHttpClient();

// Cloudinary
var csec = builder.Configuration.GetSection("Cloudinary");
var cloud = new Cloudinary(new Account(csec["CloudName"], csec["ApiKey"], csec["ApiSecret"]));
cloud.Api.Secure = true; // URLs https
builder.Services.AddSingleton(cloud);

var app = builder.Build();

// --- Pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();   // ⬅️ faltaba
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// API (attribute routed controllers: /api/...)
app.MapControllers();

// MVC convencional
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=LayoutPrincipal}/{action=Index}/{id?}");

// Razor Pages (Identity UI)
app.MapRazorPages();

app.Run();
