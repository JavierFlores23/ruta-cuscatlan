using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RutaCuscatlan.Data;
using RutaCuscatlan.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews(o => o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IReservaService, ReservaService>();
builder.Services.AddScoped<IPasarelaPago, PasarelaSimulada>();
// Pagos: "Pagos:Proveedor" = "Simulado" (demo) o "Wompi" (cobro real con Wompi El Salvador).
builder.Services.Configure<PagosOptions>(builder.Configuration.GetSection("Pagos"));
builder.Services.Configure<WompiOptions>(builder.Configuration.GetSection("Wompi"));
builder.Services.Configure<FacturacionOptions>(builder.Configuration.GetSection("Facturacion"));
builder.Services.AddHttpClient<WompiClient>(c => c.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o => { o.LoginPath = "/Account/Login"; o.AccessDeniedPath = "/Account/Login"; });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DatabaseBootstrap.InitializeAsync(db);
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
