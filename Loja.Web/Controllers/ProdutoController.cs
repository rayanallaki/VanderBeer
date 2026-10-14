using Loja.Web.Models;
using Loja.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loja.Web.Controllers;

[Authorize(Roles = "Administrador")]
[AutoValidateAntiforgeryToken]
public class ProdutosController : Controller
{
    private readonly LojaApiService _api;

    public ProdutosController(LojaApiService api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var produtos = await _api.ConsultarAsync<List<ProdutoWeb>>(
                "api/admin/produtos");

            return View(produtos);
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

        return View(new List<ProdutoWeb>());
    }
    [HttpGet]
    public IActionResult Criar()
    {
        return View("Formulario", new ProdutoFormulario());
    }

    [HttpPost]
    public async Task<IActionResult> Criar(ProdutoFormulario dados)
    {
        if (!ModelState.IsValid)
            return View("Formulario", dados);

        return await SalvarAsync(
            HttpMethod.Post,
            "api/produtos",
            new
            {
                dados.Nome,
                dados.Categoria,
                dados.Alcoolico,
                dados.Descricao,
                dados.Preco,
                dados.Estoque,
                dados.Ativo
            },
            dados);
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        try
        {
            var produtos = await _api.ConsultarAsync<List<ProdutoWeb>>(
                "api/admin/produtos");

            var produto = produtos.FirstOrDefault(produto => produto.Id == id);

            if (produto is null)
                return NotFound();

            return View("Formulario", new ProdutoFormulario
            {
                Id = produto.Id,
                Nome = produto.Nome,
                Categoria = produto.Categoria,
                Alcoolico = produto.Alcoolico,
                Descricao = produto.Descricao,
                Preco = produto.Preco,
                Estoque = produto.Estoque,
                Ativo = produto.Ativo
            });
        }
        catch (Exception)
        {
            TempData["Erro"] = "Não foi possível carregar o produto.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, ProdutoFormulario dados)
    {
        if (id <= 0 || dados.Id != id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View("Formulario", dados);

        return await SalvarAsync(
            HttpMethod.Put,
            $"api/produtos/{id}",
            new
            {
                dados.Nome,
                dados.Categoria,
                dados.Alcoolico,
                dados.Descricao,
                dados.Preco
            },
            dados);
    }

    private async Task<IActionResult> SalvarAsync(
        HttpMethod metodo,
        string rota,
        object conteudo,
        ProdutoFormulario formulario)
    {
        try
        {
            await _api.EnviarAsync(metodo, rota, conteudo);

            TempData["Sucesso"] = "Produto salvo.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException erro)
        {
            ModelState.AddModelError(string.Empty, erro.Message);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(
                string.Empty, "Não foi possível conectar à API.");
        }
        catch (TaskCanceledException)
        {
            ModelState.AddModelError(
                string.Empty, "A confirmação demorou demais. Confira a lista antes de reenviar.");
        }

        return View("Formulario", formulario);
    }
    [HttpPost]
    public async Task<IActionResult> AlterarSituacao(int id, bool? ativo)
    {
        if (id <= 0)
            return BadRequest();

        if (!ModelState.IsValid || ativo is null)
        {
            TempData["Erro"] = "A situação enviada é inválida.";
            return RedirectToAction(nameof(Index));
        }

        return await ExecutarAlteracaoAsync(
            $"api/produtos/{id}/ativo",
            new { ativo = ativo.Value });
    }

    [HttpPost]
    public async Task<IActionResult> AjustarEstoque(int id, int? ajuste)
    {
        if (id <= 0)
            return BadRequest();

        if (!ModelState.IsValid || ajuste is null || ajuste == 0)
        {
            TempData["Erro"] = "Informe um ajuste inteiro diferente de zero.";
            return RedirectToAction(nameof(Index));
        }

        return await ExecutarAlteracaoAsync(
            $"api/admin/produtos/{id}/estoque",
            new { ajuste = ajuste.Value });
    }

    private async Task<IActionResult> ExecutarAlteracaoAsync(
        string rota,
        object dados)
    {
        try
        {
            await _api.EnviarAsync(HttpMethod.Patch, rota, dados);
            TempData["Sucesso"] = "Alteração realizada.";
        }
        catch (InvalidOperationException erro)
        {
            TempData["Erro"] = erro.Message;
        }
        catch (HttpRequestException)
        {
            TempData["Erro"] =
                "Falha de comunicação. Confira a situação do produto antes de reenviar.";
        }
        catch (TaskCanceledException)
        {
            TempData["Erro"] =
                "A confirmação demorou demais. Confira o estoque antes de reenviar.";
        }

        return RedirectToAction(nameof(Index));
    }
}