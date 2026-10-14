using System.ComponentModel.DataAnnotations;

namespace Loja.Web.Models;

public class ProdutoFormulario
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a categoria.")]
    [StringLength(60)]
    public string Categoria { get; set; } = string.Empty;

    public bool Alcoolico { get; set; }

    [Required(ErrorMessage = "Informe a descrição.")]
    [StringLength(2000)]
    public string Descricao { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o preço.")]
    [Range(0, 99999999.99, ErrorMessage = "Informe um preço válido.")]
    public decimal? Preco { get; set; }

    [Range(0, int.MaxValue)]
    public int Estoque { get; set; }

    public bool Ativo { get; set; } = true;
}