namespace Loja.Web.Models;

public class PedidoWeb
{
    public int Id { get; set; }
    public DateTime DataHora { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Observacao { get; set; } = string.Empty;
    public decimal PrecoTotal { get; set; }
    public List<ItemPedidoWeb> Itens { get; set; } = new();
}

public class ItemPedidoWeb
{
    public int ProdutoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class PedidosPagina
{
    public List<PedidoWeb> Pedidos { get; set; } = new();
    public string? Status { get; set; }
    public DateTime? Inicio { get; set; }
    public DateTime? Fim { get; set; }
}