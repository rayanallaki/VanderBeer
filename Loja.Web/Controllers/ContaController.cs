using System.Security.Claims;
using Loja.Web.Models;
using Loja.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loja.Web.Controllers;

[AutoValidateAntiforgeryToken]
public class ContaController : Controller
{
    private readonly LojaApiService _api;

    public ContaController(LojaApiService api)
    {
        _api = api;
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Produtos");
        }

        return View(new LoginWeb());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginWeb dados)
    {
        if (!ModelState.IsValid)
            return View(dados);

        try
        {
            var resultado = await _api.LoginAsync(dados);

            var claims = new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    resultado.Usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, resultado.Usuario.Nome),
                new Claim(ClaimTypes.Role, "Administrador"),
                new Claim("CookieApi", resultado.CookieApi)
            };

            var identidade = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identidade),
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                });

            return RedirectToAction("Index", "Produtos");
        }
        catch (InvalidOperationException erro)
        {
            ModelState.AddModelError(string.Empty, erro.Message);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Não foi possível conectar à API.");
        }
        catch (TaskCanceledException)
        {
            ModelState.AddModelError(
                string.Empty,
                "A conexão demorou demais. Tente novamente.");
        }

        dados.Senha = string.Empty;
        ModelState.Remove(nameof(LoginWeb.Senha));

        return View(dados);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Sair()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction(nameof(Login));
    }
}