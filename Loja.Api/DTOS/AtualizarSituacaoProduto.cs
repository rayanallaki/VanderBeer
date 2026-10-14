using System.ComponentModel.DataAnnotations;

namespace Loja.Api.Dtos;

public class AtualizarSituacaoProdutoDto
{
    [Required(ErrorMessage = "Informe se o produto deve ficar ativo.")]
    public bool? Ativo { get; set; }
}