using MySqlConnector;
using Microsoft.AspNetCore.Authentication.Cookies;
using Loja.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opcoes =>
    {
        opcoes.Cookie.Name = "VanderBeer.Auth";
        opcoes.Cookie.HttpOnly = true;
        opcoes.Cookie.SameSite = SameSiteMode.Strict;

        opcoes.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        opcoes.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        opcoes.SlidingExpiration = false;
    });
builder.Services.AddScoped<PedidoService>();

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/teste-banco", async (IConfiguration configuracao) =>
    {
        var connectionString =
            configuracao.GetConnectionString("MySql")
            ?? throw new InvalidOperationException(
                "A conexão MySql não foi configurada.");

        await using var conexao = new MySqlConnection(connectionString);

        await conexao.OpenAsync();

        await using var comando = new MySqlCommand(
            "SELECT DATABASE();",
            conexao);

        var banco = await comando.ExecuteScalarAsync();

        return Results.Ok(new
        {
            mensagem = "Conexão com o MySQL realizada.",
            banco = banco?.ToString()
        });
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
