using System.ComponentModel.DataAnnotations;

namespace Loja.Api.Dtos;

public class CriarProdutoDto
{
    [Required(ErrorMessage = "Informe o nome da bebida.")]
    [StringLength(100, ErrorMessage = "O nome deve ter até 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a categoria.")]
    [StringLength(60, ErrorMessage = "A categoria deve ter até 60 caracteres.")]
    public string Categoria { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe se a bebida é alcoólica.")]
    public bool? Alcoolico { get; set; }

    [Required(ErrorMessage = "Informe a descrição.")]
    [StringLength(2000, ErrorMessage = "A descrição deve ter até 2000 caracteres.")]
    public string Descricao { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o preço.")]
    [Range(
        typeof(decimal),
        "0",
        "99999999.99",
        ErrorMessage = "O preço deve estar entre 0 e 99999999,99.")]
    public decimal? Preco { get; set; }

    [Required(ErrorMessage = "Informe o estoque.")]
    [Range(0, int.MaxValue, ErrorMessage = "O estoque não pode ser negativo.")]
    public int? Estoque { get; set; }

    public bool Ativo { get; set; } = true;
}