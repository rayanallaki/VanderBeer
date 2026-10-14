namespace Loja.Mobile.Models;
using System.Globalization;

public class PedidoApp
{
    public int Id { get; set; }
    public DateTime DataHora { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Observacao { get; set; } = string.Empty;
    public decimal PrecoTotal { get; set; }
    
    public List<ItemPedidoApp> Itens { get; set; } = new();

    public string TotalFormatado =>
    PrecoTotal.ToString("C2", CultureInfo.GetCultureInfo("pt-BR"));

    public string DataFormatada =>
        DataHora.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    public string ResumoItens =>
        string.Join(
            Environment.NewLine,
            Itens.Select(item =>
            $"{item.Quantidade} × {item.Nome}"));

            
}

public class ItemPedidoApp
{
    public string Nome { get; set; } = string.Empty;
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Subtotal { get; set; }
}