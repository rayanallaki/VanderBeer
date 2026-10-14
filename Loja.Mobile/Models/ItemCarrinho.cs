using System.Globalization;

namespace Loja.Mobile.Models;

public class ItemCarrinho
{
    public int ProdutoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public int EstoqueInformado { get; set; }

    public decimal Subtotal => PrecoUnitario * Quantidade;

    public string SubtotalFormatado =>
        Subtotal.ToString("C2", CultureInfo.GetCultureInfo("pt-BR"));
}