using System.Globalization;
using Loja.Web.Models;
using Loja.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loja.Web.Controllers;

[Authorize(Roles = "Administrador")]
[AutoValidateAntiforgeryToken]
public class PedidosController : Controller
{
    private readonly LojaApiService _api;

    public PedidosController(LojaApiService api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? status,
        DateTime? inicio,
        DateTime? fim)
    {
        var pagina = new PedidosPagina
        {
            Status = status,
            Inicio = inicio,
            Fim = fim
        };

        if (!ModelState.IsValid ||
            (inicio.HasValue && fim.HasValue &&
             inicio.Value.Date > fim.Value.Date) ||
            fim?.Date == DateTime.MaxValue.Date)
        {
            ViewBag.Erro = "Confira as datas do filtro.";
            return View(pagina);
        }

        var filtros = new List<string>();

        if (!string.IsNullOrWhiteSpace(status))
            filtros.Add("status=" + Uri.EscapeDataString(status));

        if (inicio.HasValue)
        {
            var data = DateTime.SpecifyKind(
                inicio.Value.Date, DateTimeKind.Local).ToUniversalTime();

            filtros.Add("inicio=" + Uri.EscapeDataString(
                data.ToString("O", CultureInfo.InvariantCulture)));
        }

        if (fim.HasValue)
        {
            var data = DateTime.SpecifyKind(
                fim.Value.Date.AddDays(1),
                DateTimeKind.Local).ToUniversalTime();

            filtros.Add("fim=" + Uri.EscapeDataString(
                data.ToString("O", CultureInfo.InvariantCulture)));
        }

        var rota = "api/admin/pedidos";

        if (filtros.Count > 0)
            rota += "?" + string.Join("&", filtros);

        try
        {
            pagina.Pedidos = await _api.ConsultarAsync<List<PedidoWeb>>(rota);

            var produtos = await _api.ConsultarAsync<List<ProdutoWeb>>(
                "api/admin/produtos");

            var nomes = produtos.ToDictionary(
                produto => produto.Id,
                produto => produto.Nome);

            foreach (var pedido in pagina.Pedidos)
            {
                foreach (var item in pedido.Itens)
                {
                    item.Nome = nomes.GetValueOrDefault(
                        item.ProdutoId,
                        $"Bebida {item.ProdutoId}");
                }
            }
        }
        catch (InvalidOperationException erro)
        {
            ViewBag.Erro = erro.Message;
        }
        catch (HttpRequestException)
        {
            ViewBag.Erro = "Não foi possível conectar à API.";
        }
        catch (TaskCanceledException)
        {
            ViewBag.Erro = "A consulta demorou demais.";
        }

        return View(pagina);
    }

    [HttpPost]
    public async Task<IActionResult> AtualizarStatus(int id, string status)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(status))
            return BadRequest();

        try
        {
            await _api.EnviarAsync(
                HttpMethod.Patch,
                $"api/admin/pedidos/{id}/status",
                new { status });

            TempData["Sucesso"] = "Status atualizado.";
        }
        catch (InvalidOperationException erro)
        {
            TempData["Erro"] = erro.Message;
        }
        catch (HttpRequestException)
        {
            TempData["Erro"] =
                "Falha de comunicação. Confira o status antes de reenviar.";
        }
        catch (TaskCanceledException)
        {
            TempData["Erro"] =
                "A confirmação demorou demais. Confira o status.";
        }

        return RedirectToAction(nameof(Index));
    }
}