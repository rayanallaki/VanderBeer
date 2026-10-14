using Loja.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;


var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFilter(
    "Microsoft.AspNetCore",
    LogLevel.Information);

builder.Logging.AddFilter(
    "Microsoft.AspNetCore.Antiforgery",
    LogLevel.Debug);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<LojaApiService>(cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5103/");
    cliente.Timeout = TimeSpan.FromSeconds(15);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    UseCookies = false
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opcoes =>
    {
        opcoes.Cookie.Name = "VanderBeer.Web";
        opcoes.Cookie.HttpOnly = true;
        opcoes.Cookie.SameSite = SameSiteMode.Strict;
        opcoes.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        opcoes.LoginPath = "/Conta/Login";
        opcoes.AccessDeniedPath = "/Conta/Login";
        opcoes.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        opcoes.SlidingExpiration = false;
    });

    
builder.Services.AddAuthorization();

builder.Services.Configure<RequestLocalizationOptions>(opcoes =>
{
    opcoes.SetDefaultCulture("pt-BR")
        .AddSupportedCultures("pt-BR")
        .AddSupportedUICultures("pt-BR");
});
builder.Services.AddDataProtection()
    .SetApplicationName("VanderBeer.Loja.Web");

builder.Services.AddAntiforgery(opcoes =>
{
    opcoes.Cookie.Name = "VanderBeer.Web.v2";
    opcoes.FormFieldName = "__VanderBeerToken";
    opcoes.Cookie.HttpOnly = true;
    opcoes.Cookie.SameSite = SameSiteMode.Strict;

    opcoes.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});
var app = builder.Build();
app.UseRequestLocalization();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
