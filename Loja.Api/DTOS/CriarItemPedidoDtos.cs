using System.ComponentModel.DataAnnotations;

namespace Loja.Api.Dtos;

public class CriarItemPedidoDto
{
    [Required(ErrorMessage = "Informe o produto.")]
    [Range(1, int.MaxValue, ErrorMessage = "O ID do produto deve ser positivo.")]
    public int? ProdutoId { get; set; }

    [Required(ErrorMessage = "Informe a quantidade.")]
    [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
    public int? Quantidade { get; set; }

    [Required(ErrorMessage = "Informe o preço exibido no carrinho.")]
    [Range(
        typeof(decimal),
        "0",
        "99999999.99",
        ErrorMessage = "O preço informado está fora do intervalo permitido.")]
    public decimal? PrecoEsperado { get; set; }
}