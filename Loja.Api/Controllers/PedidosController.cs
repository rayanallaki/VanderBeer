using System.Globalization;
using System.Security.Claims;
using Loja.Api.Dtos;
using Loja.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loja.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/pedidos")]
public class PedidosController : ControllerBase
{
    private readonly PedidoService _pedidoService;

    public PedidosController(PedidoService pedidoService)
    {
        _pedidoService = pedidoService;
    }

    [HttpPost]
    public async Task<ActionResult<PedidoRespostaDto>> Criar(
        [FromBody] CriarPedidoDto dados)
    {
        var identificador = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(
            identificador,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var usuarioId) || usuarioId <= 0)
        {
            return Unauthorized();
        }

        try
        {
            var pedido = await _pedidoService.CriarAsync(
                usuarioId,
                dados);

            return Ok(pedido);
        }
        catch (ArgumentException erro)
        {
            return BadRequest(new
            {
                mensagem = erro.Message
            });
        }
        catch (InvalidOperationException erro)
        {
            return Conflict(new
            {
                mensagem = erro.Message
            });
        }
    }
    [HttpGet("meus")]
    public async Task<ActionResult<List<PedidoRespostaDto>>> ListarMeus()
    {
        var identificador = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(
            identificador,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var usuarioId) || usuarioId <= 0)
        {
            return Unauthorized();
        }

        var pedidos = await _pedidoService.ListarDoUsuarioAsync(usuarioId);

        return Ok(pedidos);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPatch("/api/admin/pedidos/{id:int}/status")]
    public async Task<IActionResult> AtualizarStatus(
        int id,
        [FromBody] AtualizarStatusPedidoDto dados)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                mensagem = "O ID deve ser maior que zero."
            });
        }

        try
        {
            await _pedidoService.AtualizarStatusAsync(id, dados.Status);
            return NoContent();
        }
        catch (KeyNotFoundException erro)
        {
            return NotFound(new { mensagem = erro.Message });
        }
        catch (ArgumentException erro)
        {
            return BadRequest(new { mensagem = erro.Message });
        }
        catch (InvalidOperationException erro)
        {
            return Conflict(new { mensagem = erro.Message });
        }
    }
    [Authorize(Roles = "Administrador")]
    [HttpGet("/api/admin/pedidos")]
    public async Task<ActionResult<List<PedidoRespostaDto>>> ListarParaLoja(
        [FromQuery] string? status,
        [FromQuery] DateTimeOffset? inicio,
        [FromQuery] DateTimeOffset? fim)
    {
        var statusPermitidos = new[]
        {
            "Recebido", "EmSeparacao", "Pronto", "Finalizado", "Cancelado"
        };

        if (status is not null && !statusPermitidos.Contains(status))
        {
            return BadRequest(new { mensagem = "Status inválido." });
        }

        if (inicio.HasValue && fim.HasValue && inicio.Value >= fim.Value)
        {
            return BadRequest(new
            {
                mensagem = "O início deve ser anterior ao fim."
            });
        }

        var pedidos = await _pedidoService.ListarParaLojaAsync(
            status,
            inicio?.UtcDateTime,
            fim?.UtcDateTime);

        return Ok(pedidos);
    }
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PedidoRespostaDto>> BuscarPorId(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                mensagem = "O ID deve ser maior que zero."
            });
        }

        var identificador = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(
            identificador,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var usuarioId) || usuarioId <= 0)
        {
            return Unauthorized();
        }

        int? filtroUsuario = usuarioId;

        if (User.IsInRole("Administrador"))
        {
            filtroUsuario = null;
        }

        var pedido = await _pedidoService.BuscarPorIdAsync(id, filtroUsuario);

        if (pedido is null)
        {
            return NotFound(new
            {
                mensagem = "Pedido não encontrado."
            });
        }

        return Ok(pedido);
    }
}