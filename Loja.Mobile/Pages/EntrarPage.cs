using Loja.Mobile.Services;

namespace Loja.Mobile.Pages;

public class EntrarPage : ContentPage
{
    private readonly Entry _email = new()
    {
        Placeholder = "E-mail",
        Keyboard = Keyboard.Email
    };

    private readonly Entry _senha = new()
    {
        Placeholder = "Senha",
        IsPassword = true
    };

    private readonly Button _entrar = new() { Text = "Entrar" };
    private readonly Label _mensagem = new();

    public EntrarPage()
    {
        Title = "Entrar";

        _email.Text = Preferences.Get("ultimoEmail", string.Empty);
        _entrar.Clicked += EntrarClicked;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24),
                Spacing = 16,
                MaximumWidthRequest = 450,
                Children =
                {
                    new Label
                    {
                        Text = "Entre na VanderBeer",
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold
                    },
                    _email,
                    _senha,
                    _entrar,
                    _mensagem
                }
            }
        };
    }

    private async void EntrarClicked(object? sender, EventArgs e)
    {
        var email = _email.Text?.Trim() ?? string.Empty;
        var senha = _senha.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(senha))
        {
            _mensagem.Text = "Preencha o e-mail e a senha.";
            return;
        }

        _entrar.IsEnabled = false;
        _mensagem.Text = "Entrando...";

        try
        {
            await ApiService.Instancia.LoginAsync(email, senha);

            Preferences.Set("ultimoEmail", email);
            _senha.Text = string.Empty;

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
            _mensagem.Text = "A conexão demorou demais.";
        }
        catch (Exception)
        {
            _mensagem.Text = "Não foi possível entrar.";
        }
        finally
        {
            _entrar.IsEnabled = true;
        }
    }
}