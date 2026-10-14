using System.ComponentModel.DataAnnotations;

namespace Loja.Api.Dtos;

public class CriarPedidoDto
{
    [Required(ErrorMessage = "Informe a chave da tentativa de compra.")]
    public Guid? ChaveRequisicao { get; set; }

    [StringLength(
        300,
        ErrorMessage = "A observação deve ter até 300 caracteres.")]
    public string? Observacao { get; set; }

    [Required(ErrorMessage = "Informe os itens do pedido.")]
    [MinLength(1, ErrorMessage = "O pedido deve ter pelo menos um item.")]
    [MaxLength(100, ErrorMessage = "O pedido deve ter no máximo 100 itens.")]
    public List<CriarItemPedidoDto> Itens { get; set; } = new();
}