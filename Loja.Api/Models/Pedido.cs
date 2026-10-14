namespace Loja.Api.Models;

public class Pedido
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public DateTime DataHora { get; set; }

    public decimal PrecoTotal { get; set; }

    public string Status { get; set; } = "Recebido";

    public string Observacao { get; set; } = string.Empty;

    public string ChaveRequisicao { get; set; } = string.Empty;
}