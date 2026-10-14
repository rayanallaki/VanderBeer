using System.ComponentModel.DataAnnotations;

namespace Loja.Api.Dtos;

public class AjustarEstoqueDto
{
    [Required(ErrorMessage = "Informe a quantidade do ajuste.")]
    public int? Ajuste { get; set; }
}