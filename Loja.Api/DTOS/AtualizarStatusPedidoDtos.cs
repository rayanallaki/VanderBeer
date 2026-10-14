using System.ComponentModel.DataAnnotations;

namespace Loja.Api.Dtos;

public class AtualizarStatusPedidoDto
{
    [Required]
    [RegularExpression(
        "^(Recebido|EmSeparacao|Pronto|Finalizado|Cancelado)$",
        ErrorMessage = "Status inválido.")]
    public string Status { get; set; } = string.Empty;
}