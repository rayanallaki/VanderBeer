using System.ComponentModel.DataAnnotations;

namespace Loja.Api.Dtos;

public class AtualizarProdutoDto
{
    [Required(ErrorMessage = "Informe o nome da bebida.")]
    [StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a categoria.")]
    [StringLength(60)]
    public string Categoria { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe se a bebida é alcoólica.")]
    public bool? Alcoolico { get; set; }

    [Required(ErrorMessage = "Informe a descrição.")]
    [StringLength(2000)]
    public string Descricao { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o preço.")]
    [Range(
        typeof(decimal),
        "0",
        "99999999.99",
        ErrorMessage = "O preço deve estar entre 0 e 99999999,99.")]
    public decimal? Preco { get; set; }
}