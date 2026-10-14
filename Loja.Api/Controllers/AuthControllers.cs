using Loja.Api.Dtos;
using Loja.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;

namespace Loja.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly string _connectionString;

    public AuthController(IConfiguration configuracao)
    {
        _connectionString = configuracao.GetConnectionString("MySql")
            ?? throw new InvalidOperationException(
                "A conexão MySql não foi configurada.");
    }

    [HttpPost("login")]
    public async Task<ActionResult<UsuarioRespostaDto>> Login(
        [FromBody] LoginDto dados)
    {
        var email = dados.Email.Trim().ToLowerInvariant();

        await using var conexao = new MySqlConnection(_connectionString);

        await conexao.OpenAsync();

        const string sql = """
        SELECT Id, Nome, Email, SenhaHash, Perfil
        FROM usuarios
        WHERE Email = @email;
        """;

        await using var comando = new MySqlCommand(sql, conexao);

        comando.Parameters.AddWithValue("@email", email);

        Usuario? usuario = null;

        await using (var leitor = await comando.ExecuteReaderAsync())
        {
            if (await leitor.ReadAsync())
            {
                usuario = new Usuario
                {
                    Id = leitor.GetInt32("Id"),
                    Nome = leitor.GetString("Nome"),
                    Email = leitor.GetString("Email"),
                    SenhaHash = leitor.GetString("SenhaHash"),
                    Perfil = leitor.GetString("Perfil")
                };
            }
        }

        if (usuario is null)
        {
            return Unauthorized(new
            {
                mensagem = "E-mail ou senha incorretos."
            });
        }

        var verificador = new PasswordHasher<Usuario>();

        var resultado = verificador.VerifyHashedPassword(
            usuario,
            usuario.SenhaHash,
            dados.Senha);

        if (resultado == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                mensagem = "E-mail ou senha incorretos."
            });
        }
        var identificacoes = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Perfil)
        };

        var identidade = new ClaimsIdentity(
            identificacoes,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var usuarioAutenticado = new ClaimsPrincipal(identidade);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            usuarioAutenticado,
            new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            });

        return Ok(new UsuarioRespostaDto
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email
        });
    }
    [Authorize]
    [HttpGet("me")]
    public ActionResult<UsuarioRespostaDto> ConsultarUsuarioAtual()
    {
        var identificador = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(
            identificador,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var usuarioId))
        {
            return Unauthorized();
        }

        return Ok(new UsuarioRespostaDto
        {
            Id = usuarioId,
            Nome = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty
        });
    }
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return NoContent();
    }
}