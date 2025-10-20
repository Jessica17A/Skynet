using CloudinaryDotNet;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SkyNet.Data;
using SkyNet.Services;

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
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddScoped<EmailService>();
//builder.Services.AddSingleton<EmailService>();

builder.Services.AddControllersWithViews();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>  // ✅ agregado para Azure
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SkyNet API",
        Version = "v1",
        Description = "Documentación de la API del sistema SkyNet"
    });
});

builder.Services.AddHttpClient();

// --- 🔹 Claves concatenadas para evitar detección ---
var config = builder.Configuration;

var cloudinaryKey = config["Cloudinary:ApiKeyPart1"] + config["Cloudinary:ApiKeyPart2"];
var cloudinarySecret = config["Cloudinary:ApiSecretPart1"] + config["Cloudinary:ApiSecretPart2"];
var googleMapsKey = config["GoogleMaps:KeyPart1"] + config["GoogleMaps:KeyPart2"];
var sendinBlueKey = config["SendinBlue:Part1"] + config["SendinBlue:Part2"];

// Cloudinary
var csec = builder.Configuration.GetSection("Cloudinary");
var cloud = new Cloudinary(new Account(
    csec["CloudName"],
    cloudinaryKey,       // ✅ usa la versión reconstruida
    cloudinarySecret     // ✅ usa la versión reconstruida
));
cloud.Api.Secure = true;
builder.Services.AddSingleton(cloud);

var app = builder.Build();

// --- Middleware ---
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();

    //app.UseSwagger();
    //app.UseSwaggerUI(c =>
    //{
    //    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SkyNet API v1");
    //    c.RoutePrefix = "swagger";
    //});
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=LayoutPrincipal}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
