using System.Globalization;
using Loja.Mobile.Models;
using Loja.Mobile.Services;

namespace Loja.Mobile.Pages;

public partial class CarrinhoPage : ContentPage
{   
    private bool _enviando;
    public CarrinhoPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AtualizarTela();
    }

    private void AtualizarTela()
    {
        ItensView.ItemsSource = CarrinhoService.Instancia.ObterItens();

        var total = CarrinhoService.Instancia.Total.ToString(
            "C2",
            CultureInfo.GetCultureInfo("pt-BR"));

        TotalLabel.Text = $"Total estimado: {total}";
    }

    private async void AlterarClicked(object? sender, EventArgs e)
    {
        if (sender is not Button botao ||
            botao.BindingContext is not ItemCarrinho item)
        {
            return;
        }

        var resposta = await DisplayPromptAsync(
            "Alterar quantidade",
            item.Nome,
            accept: "Salvar",
            cancel: "Cancelar",
            keyboard: Keyboard.Numeric,
            initialValue: item.Quantidade.ToString());

        if (resposta is null)
            return;

        if (!int.TryParse(resposta, out var quantidade) || quantidade <= 0)
        {
            await DisplayAlertAsync(
                "Quantidade inválida",
                "Informe um número inteiro maior que zero.",
                "OK");

            return;
        }

        try
        {
            CarrinhoService.Instancia.AlterarQuantidade(
                item.ProdutoId,
                quantidade);

            AtualizarTela();
        }
        catch (InvalidOperationException erro)
        {
            await DisplayAlertAsync("Carrinho", erro.Message, "OK");
        }
    }

    private async void RemoverClicked(object? sender, EventArgs e)
    {
        if (sender is not Button botao ||
            botao.BindingContext is not ItemCarrinho item)
        {
            return;
        }

        try
        {
            CarrinhoService.Instancia.Remover(item.ProdutoId);
            AtualizarTela();
        }
        catch (InvalidOperationException erro)
        {
            await DisplayAlertAsync("Carrinho", erro.Message, "OK");
        }
    }
    private async void FinalizarClicked(object? sender, EventArgs e)
    {
        if (ApiService.Instancia.UsuarioAtual is null)
        {
            await Navigation.PushAsync(new EntrarPage());
            return;
        }

        var carrinho = CarrinhoService.Instancia;
        var itens = carrinho.ObterItens();

        if (itens.Count == 0)
        {
            await DisplayAlertAsync("Carrinho", "Adicione uma bebida.", "OK");
            return;
        }

        if (!carrinho.TentativaPendente)
        {
            var confirmar = await DisplayAlertAsync(
                "Confirmar compra",
                TotalLabel.Text + "\nDeseja enviar o pedido?",
                "Confirmar",
                "Voltar");

            if (!confirmar)
                return;

            carrinho.PrepararEnvio(ObservacaoEntry.Text ?? string.Empty);
        }

        _enviando = true;
        FinalizarButton.IsEnabled = false;
        ItensView.IsEnabled = false;
        ObservacaoEntry.IsEnabled = false;
        MensagemLabel.Text = "Enviando pedido...";

        try
        {
            var pedido = await ApiService.Instancia.EnviarPedidoAsync(
                carrinho.ChaveCompra,
                carrinho.ObservacaoCompra,
                itens);

            carrinho.Limpar();
            ObservacaoEntry.Text = string.Empty;
            AtualizarTela();

            MensagemLabel.Text = $"Pedido {pedido.Id} registrado.";

            var total = pedido.PrecoTotal.ToString(
                "C2", CultureInfo.GetCultureInfo("pt-BR"));

            await DisplayAlertAsync(
                "Pedido confirmado",
                $"Número: {pedido.Id}\nTotal: {total}\nStatus: {pedido.Status}",
                "OK");
        }
        catch (ApiOperacaoException erro)
        {
            if (erro.CodigoHttp < 500)
                carrinho.LiberarTentativa();

            MensagemLabel.Text = erro.Message;
        }
        catch (Exception)
        {
            MensagemLabel.Text =
                "Não foi possível obter a confirmação. " +
                "Tente enviar novamente sem iniciar outra compra.";
        }
        finally
        {
            _enviando = false;
            FinalizarButton.IsEnabled = true;
            ItensView.IsEnabled = !carrinho.TentativaPendente;
            ObservacaoEntry.IsEnabled = !carrinho.TentativaPendente;
        }
    }
}