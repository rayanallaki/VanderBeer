using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Loja.Web.Models;

namespace Loja.Web.Services;

public class LojaApiService
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _contexto;

    public LojaApiService(
        HttpClient http,
        IHttpContextAccessor contexto)
    {
        _http = http;
        _contexto = contexto;
    }

    public async Task<(UsuarioWeb Usuario, string CookieApi)> LoginAsync(
        LoginWeb dados)
    {
        using var resposta = await _http.PostAsJsonAsync(
            "api/auth/login",
            new { email = dados.Email, senha = dados.Senha });

        if (!resposta.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                "Não foi possível entrar. Confira e-mail e senha.");
        }

        var usuario = await resposta.Content.ReadFromJsonAsync<UsuarioWeb>()
            ?? throw new InvalidOperationException("Resposta de login inválida.");

        if (!resposta.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            throw new InvalidOperationException(
                "A API não forneceu a autenticação.");
        }

        var cookieApi = string.Join(
            "; ",
            cookies.Select(cookie => cookie.Split(';')[0])
                .Where(cookie =>
                    cookie.StartsWith("VanderBeer.Auth=", StringComparison.Ordinal) ||
                    cookie.StartsWith("VanderBeer.AuthC", StringComparison.Ordinal)));

        if (string.IsNullOrEmpty(cookieApi))
        {
            throw new InvalidOperationException("Cookie de autenticação ausente.");
        }

        using var teste = new HttpRequestMessage(
            HttpMethod.Get,
            "api/admin/produtos");

        teste.Headers.Add("Cookie", cookieApi);

        using var acesso = await _http.SendAsync(teste);

        if (acesso.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException(
                "Esta área é exclusiva para administradores.");
        }

        acesso.EnsureSuccessStatusCode();

        return (usuario, cookieApi);
    }

    public async Task<T> ConsultarAsync<T>(string rota)
    {
        using var requisicao = CriarRequisicao(HttpMethod.Get, rota);
        using var resposta = await _http.SendAsync(requisicao);

        await VerificarRespostaAsync(resposta);

        return await resposta.Content.ReadFromJsonAsync<T>()
            ?? throw new InvalidOperationException("Resposta da API inválida.");
    }

    public async Task EnviarAsync(
        HttpMethod metodo,
        string rota,
        object? dados = null)
    {
        using var requisicao = CriarRequisicao(metodo, rota);

        if (dados is not null)
            requisicao.Content = JsonContent.Create(dados);

        using var resposta = await _http.SendAsync(requisicao);

        await VerificarRespostaAsync(resposta);
    }

    private HttpRequestMessage CriarRequisicao(
        HttpMethod metodo,
        string rota)
    {
        var cookie = _contexto.HttpContext?.User
            .FindFirstValue("CookieApi");

        if (string.IsNullOrEmpty(cookie))
        {
            throw new InvalidOperationException(
                "Entre novamente na conta administrativa.");
        }

        var requisicao = new HttpRequestMessage(metodo, rota);
        requisicao.Headers.Add("Cookie", cookie);

        return requisicao;
    }

    private static async Task VerificarRespostaAsync(
        HttpResponseMessage resposta)
    {
        if (resposta.IsSuccessStatusCode)
            return;

        if (resposta.StatusCode == HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("Sessão expirada. Entre novamente.");

        if (resposta.StatusCode == HttpStatusCode.Forbidden)
            throw new InvalidOperationException("Acesso não permitido.");

        if ((int)resposta.StatusCode >= 500)
            throw new InvalidOperationException("A loja não conseguiu concluir a operação.");

        var texto = await resposta.Content.ReadAsStringAsync();

        try
        {
            using var json = JsonDocument.Parse(texto);

            if (json.RootElement.TryGetProperty("mensagem", out var mensagem))
            {
                throw new InvalidOperationException(
                    mensagem.GetString() ?? "Operação recusada.");
            }

            if (json.RootElement.TryGetProperty("errors", out var erros))
            {
                foreach (var campo in erros.EnumerateObject())
                {
                    foreach (var erro in campo.Value.EnumerateArray())
                    {
                        throw new InvalidOperationException(
                            erro.GetString() ?? "Confira os dados informados.");
                    }
                }
            }
        }
        catch (JsonException)
        {
        }

        throw new InvalidOperationException("Não foi possível concluir a operação.");
    }
}