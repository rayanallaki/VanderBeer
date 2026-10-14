namespace Loja.Api.Models;

public class Produto
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    public bool Alcoolico { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public decimal Preco { get; set; }

    public int Estoque { get; set; }

    public bool Ativo { get; set; } = true;
}