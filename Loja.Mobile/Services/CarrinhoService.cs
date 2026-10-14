using Loja.Mobile.Models;

namespace Loja.Mobile.Services;

public class CarrinhoService
{
    public static CarrinhoService Instancia { get; } = new();

    private readonly List<ItemCarrinho> _itens = new();

    public List<ItemCarrinho> ObterItens()
    {
        return _itens.ToList();
    }

    public decimal Total => _itens.Sum(item => item.Subtotal);

    public void Adicionar(Produto produto, int quantidade)
    {
        VerificarEdicaoPermitida();
        if (!produto.Ativo || quantidade <= 0)
        {
            throw new InvalidOperationException(
                "Produto ou quantidade inválidos.");
        }

        var existente = _itens.FirstOrDefault(
            item => item.ProdutoId == produto.Id);

        var novaQuantidade =
            (long)(existente?.Quantidade ?? 0) + quantidade;

        if (novaQuantidade > produto.Estoque)
        {
            throw new InvalidOperationException(
                "A quantidade ultrapassa o estoque informado no catálogo.");
        }

        if (existente is null)
        {
            _itens.Add(new ItemCarrinho
            {
                ProdutoId = produto.Id,
                Nome = produto.Nome,
                Quantidade = quantidade,
                PrecoUnitario = produto.Preco,
                EstoqueInformado = produto.Estoque
            });
        }
        else
        {
            existente.Quantidade = (int)novaQuantidade;
            existente.PrecoUnitario = produto.Preco;
            existente.EstoqueInformado = produto.Estoque;
        }
    }
    public void AlterarQuantidade(int produtoId, int quantidade)
    {
        VerificarEdicaoPermitida();
        var item = _itens.FirstOrDefault(
            item => item.ProdutoId == produtoId);

        if (item is null)
            throw new InvalidOperationException("Item não encontrado.");

        if (quantidade <= 0)
            throw new InvalidOperationException(
                "A quantidade deve ser maior que zero.");

        if (quantidade > item.EstoqueInformado)
            throw new InvalidOperationException(
                "A quantidade ultrapassa o estoque informado no catálogo.");

        item.Quantidade = quantidade;
    }

    public void Remover(int produtoId)
    {
        VerificarEdicaoPermitida();
        _itens.RemoveAll(item => item.ProdutoId == produtoId);
    }

    public void Limpar()
    {
        _itens.Clear();
        TentativaPendente = false;
        ObservacaoCompra = string.Empty;
        ChaveCompra = Guid.NewGuid();
    }
    public Guid ChaveCompra { get; private set; } = Guid.NewGuid();

    public bool TentativaPendente { get; private set; }

    public string ObservacaoCompra { get; private set; } = string.Empty;

    public void PrepararEnvio(string observacao)
    {
        if (TentativaPendente)
            return;

        ObservacaoCompra = observacao.Trim();
        TentativaPendente = true;
    }

    public void LiberarTentativa()
    {
        TentativaPendente = false;
    }

    private void VerificarEdicaoPermitida()
    {
        if (TentativaPendente)
        {
            throw new InvalidOperationException(
                "Há uma compra aguardando confirmação. " +
                "Volte ao carrinho e tente enviar novamente.");
        }
    }
}