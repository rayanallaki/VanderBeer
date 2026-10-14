using System.Globalization;
using System.Text.Json.Serialization;
using SQLite;

namespace Loja.Mobile.Models;

public class Produto
{
    [PrimaryKey]
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public bool Alcoolico { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int Estoque { get; set; }
    public bool Ativo { get; set; }

    [JsonIgnore]
    public long PrecoCentavos { get; set; }

    [Ignore]
    public decimal Preco
    {
        get => PrecoCentavos / 100m;
        set => PrecoCentavos = checked(
            (long)decimal.Round(value * 100m, 0));
    }

    [Ignore]
    public string PrecoFormatado =>
        Preco.ToString("C2", CultureInfo.GetCultureInfo("pt-BR"));

    [Ignore]
    public string TipoBebida =>
        Alcoolico ? "Alcoólica" : "Não alcoólica";
}