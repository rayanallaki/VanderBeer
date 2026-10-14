using System.Net;
using System.Net.Http.Json;
using Loja.Mobile.Models;
using System.Text.Json;

namespace Loja.Mobile.Services;

public class ApiService
{
    public static ApiService Instancia { get; } = new();

    private readonly HttpClient _http;

    private ApiService()
    {
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };

        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5103/"),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public async Task<List<Produto>> ObterProdutosAsync()
    {
        return await _http.GetFromJsonAsync<List<Produto>>(
            "api/produtos") ?? new List<Produto>();
    }
    public async Task<List<Produto>> ObterCatalogoCompletoAsync()
    {
        return await _http.GetFromJsonAsync<List<Produto>>(
            "api/produtos/sincronizacao")
            ?? throw new InvalidOperationException(
                "A API retornou um catálogo inválido.");
    }
    public async Task<PedidoApp> EnviarPedidoAsync(
    Guid chave,
    string observacao,
    List<ItemCarrinho> itens)
{
    var dados = new
    {
        chaveRequisicao = chave,
        observacao,
        itens = itens.Select(item => new
        {
            produtoId = item.ProdutoId,
            quantidade = item.Quantidade,
            precoEsperado = item.PrecoUnitario
        }).ToArray()
    };

    using var resposta = await _http.PostAsJsonAsync(
        "api/pedidos",
        dados);

    if (!resposta.IsSuccessStatusCode)
    {
        throw new InvalidOperationException(
            await ObterMensagemErroAsync(resposta));
    }

    return await resposta.Content.ReadFromJsonAsync<PedidoApp>()
        ?? throw new InvalidOperationException(
            "Não foi possível ler a confirmação. " +
            "Consulte o histórico antes de iniciar outra compra.");
}

public async Task<List<PedidoApp>> ObterMeusPedidosAsync()
{
    using var resposta = await _http.GetAsync("api/pedidos/meus");

    if (!resposta.IsSuccessStatusCode)
    {
        throw new ApiOperacaoException(
            (int)resposta.StatusCode,
                await ObterMensagemErroAsync(resposta));
    }

    return await resposta.Content.ReadFromJsonAsync<List<PedidoApp>>()
        ?? throw new InvalidOperationException(
            "Não foi possível ler o histórico.");
}

private static async Task<string> ObterMensagemErroAsync(
    HttpResponseMessage resposta)
    {
        if (resposta.StatusCode == HttpStatusCode.Unauthorized)
            return "Entre na sua conta para continuar.";

        if (resposta.StatusCode == HttpStatusCode.Forbidden)
            return "Sua conta não tem permissão para essa operação.";

        if ((int)resposta.StatusCode >= 500)
            return "A loja não conseguiu concluir a operação. Tente novamente.";

        var texto = await resposta.Content.ReadAsStringAsync();

        try
        {
            using var json = JsonDocument.Parse(texto);

            if (json.RootElement.TryGetProperty("mensagem", out var mensagem))
            {
                return mensagem.GetString()
                    ?? "Não foi possível concluir a operação.";
            }

            if (json.RootElement.TryGetProperty("errors", out var erros))
            {
                foreach (var campo in erros.EnumerateObject())
                {
                    foreach (var erro in campo.Value.EnumerateArray())
                    {
                        return erro.GetString()
                            ?? "Confira os dados informados.";
                    }
                }
            }
        }
        catch (JsonException)
        {
            // A resposta pode não conter JSON.
        }

        return "Não foi possível concluir a operação.";
    }
    public async Task CadastrarUsuarioAsync(CadastroUsuario dados)
    {
        using var resposta = await _http.PostAsJsonAsync(
            "api/usuarios",
            dados);

        if (!resposta.IsSuccessStatusCode)
        {
            throw new ApiOperacaoException(
                (int)resposta.StatusCode,
                await ObterMensagemErroAsync(resposta));
        }
    }
    public UsuarioApp? UsuarioAtual { get; private set; }

    public async Task<UsuarioApp> LoginAsync(string email, string senha)
    {
        using var resposta = await _http.PostAsJsonAsync(
            "api/auth/login",
            new { email = email.Trim(), senha });

        if (!resposta.IsSuccessStatusCode)
        {
            if (resposta.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException(
                    "E-mail ou senha incorretos.");
            }

            throw new InvalidOperationException(
                await ObterMensagemErroAsync(resposta));
        }

        var usuario = await resposta.Content.ReadFromJsonAsync<UsuarioApp>()
            ?? throw new InvalidOperationException(
                "Não foi possível identificar o usuário.");

        UsuarioAtual = usuario;
        return usuario;
    }

    public async Task LogoutAsync()
    {
        using var resposta = await _http.PostAsync("api/auth/logout", null);

        if (!resposta.IsSuccessStatusCode &&
            resposta.StatusCode != HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException(
                await ObterMensagemErroAsync(resposta));
        }

        UsuarioAtual = null;
    }
}
public class ApiOperacaoException : InvalidOperationException
{
    public int CodigoHttp { get; }

    public ApiOperacaoException(int codigoHttp, string mensagem)
        : base(mensagem)
    {
        CodigoHttp = codigoHttp;
    }
}