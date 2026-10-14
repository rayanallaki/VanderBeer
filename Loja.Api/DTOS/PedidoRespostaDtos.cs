namespace Loja.Api.Dtos;

public class PedidoRespostaDto
{
    public int Id { get; set; }

    public DateTime DataHora { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Observacao { get; set; } = string.Empty;

    public decimal PrecoTotal { get; set; }

    public List<ItemPedidoRespostaDto> Itens { get; set; } = new();
}

public class ItemPedidoRespostaDto
{
    public int ProdutoId { get; set; }

    public int Quantidade { get; set; }

    public decimal PrecoUnitario { get; set; }

    public decimal Subtotal { get; set; }
}