using Loja.Mobile.Services;

namespace Loja.Mobile.Pages;

public partial class PedidosPage : ContentPage
{
    private bool _carregando;

    public PedidosPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarAsync();
    }

    private async void AtualizarClicked(object? sender, EventArgs e)
    {
        await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        if (_carregando)
            return;

        _carregando = true;
        AtualizarButton.IsEnabled = false;
        MensagemLabel.Text = "Consultando pedidos...";

        try
        {
            var pedidos = await ApiService.Instancia.ObterMeusPedidosAsync();

            foreach (var pedido in pedidos)
            {
                foreach (var item in pedido.Itens)
                {
                    var produto = await CatalogoLocalService.Instancia
                        .ObterProdutoAsync(item.ProdutoId);

                    item.Nome = produto?.Nome
                        ?? $"Bebida {item.ProdutoId}";
                }
            }

            PedidosView.ItemsSource = pedidos;

            MensagemLabel.Text = pedidos.Count == 0
                ? "Você ainda não tem pedidos."
                : $"{pedidos.Count} pedidos encontrados.";
        }
        catch (InvalidOperationException erro)
        {
            MensagemLabel.Text = erro.Message;
        }
        catch (HttpRequestException)
        {
            MensagemLabel.Text = "Não foi possível conectar à loja.";
        }
        catch (TaskCanceledException)
        {
            MensagemLabel.Text = "A consulta demorou demais.";
        }
        catch (Exception)
        {
            MensagemLabel.Text = "Não foi possível carregar os pedidos.";
        }
        finally
        {
            _carregando = false;
            AtualizarButton.IsEnabled = true;
        }
    }
}