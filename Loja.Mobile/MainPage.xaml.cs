using Loja.Mobile.Services;
using Loja.Mobile.Models;
using Loja.Mobile.Pages;

namespace Loja.Mobile;

public partial class MainPage : ContentPage
{
    private bool _carregando;
    private readonly IDispatcherTimer _timer;

    public MainPage()
    {
        InitializeComponent();

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMinutes(5);

        _timer.Tick += async (_, _) =>
        {
            await CarregarProdutosAsync(false);
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
		ContaButton.Text = ApiService.Instancia.UsuarioAtual is null
			? "Entrar"
			: "Minha conta";

        _timer.Start();
        await CarregarProdutosAsync(false);
    }

    protected override void OnDisappearing()
    {
        _timer.Stop();
        base.OnDisappearing();
    }

    private async void AtualizarClicked(object? sender, EventArgs e)
    {
        await CarregarProdutosAsync(true);
    }

    private async Task CarregarProdutosAsync(bool forcarAtualizacao)
    {
        if (_carregando)
            return;

        _carregando = true;
        AtualizarButton.IsEnabled = false;

        var temCatalogoSalvo = false;

        try
        {
            var local = CatalogoLocalService.Instancia;

            var produtosSalvos = await local.ListarAtivosAsync();
            var ultimaSincronizacao =
                await local.ObterUltimaSincronizacaoAsync();

            temCatalogoSalvo = ultimaSincronizacao.HasValue;

            ProdutosView.ItemsSource = produtosSalvos;

            MensagemLabel.Text = temCatalogoSalvo
                ? $"{produtosSalvos.Count} bebidas no catálogo salvo."
                : "Carregando catálogo pela primeira vez...";

            var precisaAtualizar =
                forcarAtualizacao ||
                !ultimaSincronizacao.HasValue ||
                DateTime.UtcNow - ultimaSincronizacao.Value
                    >= TimeSpan.FromMinutes(5);

            if (!precisaAtualizar)
                return;

            MensagemLabel.Text = temCatalogoSalvo
                ? "Catálogo salvo disponível. Buscando atualizações..."
                : "Buscando bebidas...";

            var produtosRecebidos =
                await ApiService.Instancia.ObterCatalogoCompletoAsync();

            await local.SalvarCatalogoAsync(produtosRecebidos);

            var produtosAtualizados = await local.ListarAtivosAsync();

            ProdutosView.ItemsSource = produtosAtualizados;
            MensagemLabel.Text =
                $"{produtosAtualizados.Count} bebidas. Catálogo atualizado.";
        }
        catch (HttpRequestException)
        {
            MensagemLabel.Text = temCatalogoSalvo
                ? "Sem conexão com a loja. Exibindo o catálogo salvo."
                : "Conecte-se à loja para baixar o primeiro catálogo.";
        }
        catch (TaskCanceledException)
        {
            MensagemLabel.Text = temCatalogoSalvo
                ? "A atualização demorou demais. Catálogo salvo mantido."
                : "A consulta demorou demais. Tente atualizar novamente.";
        }
        catch (Exception)
        {
            MensagemLabel.Text = temCatalogoSalvo
                ? "Não foi possível atualizar. Catálogo salvo mantido."
                : "Não foi possível carregar o catálogo.";
        }
        finally
        {
            _carregando = false;
            AtualizarButton.IsEnabled = true;
        }
    }
	private async void AdicionarClicked(object? sender, EventArgs e)
	{
		if (sender is not Button botao ||
			botao.BindingContext is not Produto produto)
		{
			return;
		}

		var resposta = await DisplayPromptAsync(
			"Adicionar bebida",
			$"Quantas unidades de {produto.Nome}?",
			accept: "Adicionar",
			cancel: "Cancelar",
			keyboard: Keyboard.Numeric,
			initialValue: "1");

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
			CarrinhoService.Instancia.Adicionar(produto, quantidade);

			await DisplayAlertAsync(
				"Carrinho",
				"Bebida adicionada ao carrinho.",
				"OK");
		}
		catch (InvalidOperationException erro)
		{
			await DisplayAlertAsync("Carrinho", erro.Message, "OK");
		}
	}
	private async void CarrinhoClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new CarrinhoPage());
	}
	private async void PedidosClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new PedidosPage());
	}
	private async void CadastroClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new CadastroPage());
	}
	private async void ContaClicked(object? sender, EventArgs e)
	{
		var api = ApiService.Instancia;

		if (api.UsuarioAtual is null)
		{
			await Navigation.PushAsync(new EntrarPage());
			return;
		}

		if (CarrinhoService.Instancia.TentativaPendente)
		{
			await DisplayAlertAsync(
				"Compra pendente",
				"Obtenha a confirmação da compra antes de sair da conta.",
				"OK");

			return;
		}

		var sair = await DisplayAlertAsync(
			"Minha conta",
			$"{api.UsuarioAtual.Nome}\n{api.UsuarioAtual.Email}",
			"Sair",
			"Voltar");

		if (!sair)
			return;

		try
		{
			await api.LogoutAsync();
			CarrinhoService.Instancia.Limpar();
			ContaButton.Text = "Entrar";
		}
		catch (Exception)
		{
			await DisplayAlertAsync(
				"Minha conta",
				"Não foi possível encerrar a sessão. Tente novamente.",
				"OK");
		}
	}
}