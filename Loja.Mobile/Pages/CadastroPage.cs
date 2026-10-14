using Loja.Mobile.Models;
using Loja.Mobile.Services;

namespace Loja.Mobile.Pages;

public class CadastroPage : ContentPage
{
    private readonly Entry _nome = new()
        { Placeholder = "Nome completo", MaxLength = 100 };

    private readonly Entry _documento = new()
        { Placeholder = "Documento", MaxLength = 20 };

    private readonly Entry _rua = new()
        { Placeholder = "Rua", MaxLength = 100 };

    private readonly Entry _bairro = new()
        { Placeholder = "Bairro", MaxLength = 60 };

    private readonly Entry _cep = new()
        { Placeholder = "CEP", MaxLength = 9, Keyboard = Keyboard.Numeric };

    private readonly Entry _numero = new()
        { Placeholder = "Número", MaxLength = 10 };

    private readonly Entry _complemento = new()
        { Placeholder = "Complemento (opcional)", MaxLength = 100 };

    private readonly Entry _telefone = new()
        { Placeholder = "Telefone", MaxLength = 20, Keyboard = Keyboard.Telephone };

    private readonly Entry _email = new()
        { Placeholder = "E-mail", MaxLength = 150, Keyboard = Keyboard.Email };

    private readonly Entry _senha = new()
        { Placeholder = "Senha (mínimo 8 caracteres)", IsPassword = true, MaxLength = 128 };

    private readonly Button _cadastrar = new()
        { Text = "Criar conta" };

    private readonly Label _mensagem = new();

    public CadastroPage()
    {
        Title = "Criar conta";

        _cadastrar.Clicked += CadastrarClicked;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24),
                Spacing = 12,
                MaximumWidthRequest = 500,
                Children =
                {
                    new Label
                    {
                        Text = "Cadastro VanderBeer",
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold
                    },
                    _nome,
                    _documento,
                    _rua,
                    _bairro,
                    _cep,
                    _numero,
                    _complemento,
                    _telefone,
                    _email,
                    _senha,
                    _cadastrar,
                    _mensagem
                }
            }
        };
    }

    private async void CadastrarClicked(object? sender, EventArgs e)
    {
        _cadastrar.IsEnabled = false;
        _mensagem.Text = "Criando conta...";

        try
        {
            var dados = new CadastroUsuario
            {
                Nome = _nome.Text?.Trim() ?? string.Empty,
                Documento = _documento.Text?.Trim() ?? string.Empty,
                Rua = _rua.Text?.Trim() ?? string.Empty,
                Bairro = _bairro.Text?.Trim() ?? string.Empty,
                Cep = _cep.Text?.Trim() ?? string.Empty,
                Numero = _numero.Text?.Trim() ?? string.Empty,
                Complemento = _complemento.Text?.Trim() ?? string.Empty,
                Telefone = _telefone.Text?.Trim() ?? string.Empty,
                Email = _email.Text?.Trim() ?? string.Empty,
                Senha = _senha.Text ?? string.Empty
            };

            await ApiService.Instancia.CadastrarUsuarioAsync(dados);

            _senha.Text = string.Empty;

            await DisplayAlertAsync(
                "Conta criada",
                "Seu cadastro foi realizado. Entre na sua conta para comprar.",
                "OK");

            await Navigation.PopAsync();
        }
        catch (InvalidOperationException erro)
        {
            _mensagem.Text = erro.Message;
        }
        catch (HttpRequestException)
        {
            _mensagem.Text = "Não foi possível conectar à loja.";
        }
        catch (TaskCanceledException)
        {
            _mensagem.Text =
                "A confirmação demorou demais. Ao tentar novamente, " +
                "o sistema verificará se os dados já estão cadastrados.";
        }
        catch (Exception)
        {
            _mensagem.Text = "Não foi possível concluir o cadastro.";
        }
        finally
        {
            _cadastrar.IsEnabled = true;
        }
    }
}