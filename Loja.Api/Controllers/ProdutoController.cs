using Loja.Api.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Loja.Api.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace Loja.Api.Controllers;

[ApiController]
[Route("api/produtos")]
public class ProdutosController : ControllerBase
{
    private readonly string _connectionString;

    public ProdutosController(IConfiguration configuracao)
    {
        _connectionString = configuracao.GetConnectionString("MySql")
            ?? throw new InvalidOperationException(
                "A conexão MySql não foi configurada.");
    }
    
    [HttpGet]
    public async Task<ActionResult<List<Produto>>> Listar()
    {
        var produtos = new List<Produto>();

        await using var conexao = new MySqlConnection(_connectionString);

        await conexao.OpenAsync();

        const string sql = """
            SELECT Id, Nome, Categoria, Alcoolico,
                   Descricao, Preco, Estoque, Ativo
            FROM produtos
            WHERE Ativo = TRUE
            ORDER BY Nome;
            """;

        await using var comando = new MySqlCommand(sql, conexao);

        await using var leitor = await comando.ExecuteReaderAsync();

        while (await leitor.ReadAsync())
        {
            var produto = new Produto
            {
                Id = leitor.GetInt32("Id"),
                Nome = leitor.GetString("Nome"),
                Categoria = leitor.GetString("Categoria"),
                Alcoolico = leitor.GetBoolean("Alcoolico"),
                Descricao = leitor.GetString("Descricao"),
                Preco = leitor.GetDecimal("Preco"),
                Estoque = leitor.GetInt32("Estoque"),
                Ativo = leitor.GetBoolean("Ativo")
            };

            produtos.Add(produto);
        }

        return Ok(produtos);
    }
    
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Produto>> BuscarPorId(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                mensagem = "O ID deve ser maior que zero."
            });
        }

        await using var conexao = new MySqlConnection(_connectionString);

        await conexao.OpenAsync();

        const string sql = """
            SELECT Id, Nome, Categoria, Alcoolico,
                Descricao, Preco, Estoque, Ativo
            FROM produtos
            WHERE Id = @id AND Ativo = TRUE;
            """;

        await using var comando = new MySqlCommand(sql, conexao);

        comando.Parameters.AddWithValue("@id", id);

        await using var leitor = await comando.ExecuteReaderAsync();

        if (!await leitor.ReadAsync())
        {
            return NotFound(new
            {
                mensagem = "Produto não encontrado."
            });
        }

        var produto = new Produto
        {
            Id = leitor.GetInt32("Id"),
            Nome = leitor.GetString("Nome"),
            Categoria = leitor.GetString("Categoria"),
            Alcoolico = leitor.GetBoolean("Alcoolico"),
            Descricao = leitor.GetString("Descricao"),
            Preco = leitor.GetDecimal("Preco"),
            Estoque = leitor.GetInt32("Estoque"),
            Ativo = leitor.GetBoolean("Ativo")
        };

        return Ok(produto);
    }
    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<ActionResult<Produto>> Cadastrar(
        [FromBody] CriarProdutoDto dados
        )
    {


        var preco = dados.Preco!.Value;

        if (decimal.Round(preco, 2) != preco)
        {
            return BadRequest(new
            {
                mensagem = "O preço deve ter no máximo duas casas decimais."
            });
        }

        var produto = new Produto
        {
            Nome = dados.Nome.Trim(),
            Categoria = dados.Categoria.Trim(),
            Alcoolico = dados.Alcoolico!.Value,
            Descricao = dados.Descricao.Trim(),
            Preco = preco,
            Estoque = dados.Estoque!.Value,
            Ativo = dados.Ativo
        };

        await using var conexao = new MySqlConnection(_connectionString);

        await conexao.OpenAsync();

        const string sql = """
            INSERT INTO produtos
                (Nome, Categoria, Alcoolico, Descricao, Preco, Estoque, Ativo)
            VALUES
                (@nome, @categoria, @alcoolico, @descricao,
                @preco, @estoque, @ativo);
            """;

        await using var comando = new MySqlCommand(sql, conexao);

        comando.Parameters.AddWithValue("@nome", produto.Nome);
        comando.Parameters.AddWithValue("@categoria", produto.Categoria);
        comando.Parameters.AddWithValue("@alcoolico", produto.Alcoolico);
        comando.Parameters.AddWithValue("@descricao", produto.Descricao);
        comando.Parameters.AddWithValue("@preco", produto.Preco);
        comando.Parameters.AddWithValue("@estoque", produto.Estoque);
        comando.Parameters.AddWithValue("@ativo", produto.Ativo);

        await comando.ExecuteNonQueryAsync();

        produto.Id = checked((int)comando.LastInsertedId);

        return StatusCode(StatusCodes.Status201Created, produto);
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(
        int id,
        [FromBody] AtualizarProdutoDto dados
        )
    {

        if (id <= 0)
        {
            return BadRequest(new
            {
                mensagem = "O ID deve ser maior que zero."
            });
        }

        var preco = dados.Preco!.Value;

        if (decimal.Round(preco, 2) != preco)
        {
            return BadRequest(new
            {
                mensagem = "O preço deve ter no máximo duas casas decimais."
            });
        }

        await using var conexao = new MySqlConnection(_connectionString);

        await conexao.OpenAsync();

        const string sql = """
            UPDATE produtos
            SET Nome = @nome,
                Categoria = @categoria,
                Alcoolico = @alcoolico,
                Descricao = @descricao,
                Preco = @preco
            WHERE Id = @id;
            """;

        await using var comando = new MySqlCommand(sql, conexao);

        comando.Parameters.AddWithValue("@id", id);
        comando.Parameters.AddWithValue("@nome", dados.Nome.Trim());
        comando.Parameters.AddWithValue("@categoria", dados.Categoria.Trim());
        comando.Parameters.AddWithValue("@alcoolico", dados.Alcoolico!.Value);
        comando.Parameters.AddWithValue("@descricao", dados.Descricao.Trim());
        comando.Parameters.AddWithValue("@preco", preco);

        var linhasEncontradas = await comando.ExecuteNonQueryAsync();

        if (linhasEncontradas == 0)
        {
            // Confere a existência caso o banco informe zero alterações.
            await using var consulta = new MySqlCommand(
                "SELECT COUNT(*) FROM produtos WHERE Id = @id;",
                conexao);

            consulta.Parameters.AddWithValue("@id", id);

            var quantidade = Convert.ToInt32(
                await consulta.ExecuteScalarAsync());

            if (quantidade == 0)
            {
                return NotFound(new
                {
                    mensagem = "Produto não encontrado."
                });
            }
        }

        

        return NoContent();
    }
    [Authorize(Roles = "Administrador")]
    [HttpPatch("{id:int}/ativo")]
    public async Task<IActionResult> AtualizarSituacao(
        int id,
        [FromBody] AtualizarSituacaoProdutoDto dados
        )
    {   

        if (id <= 0)
        {
            return BadRequest(new
            {
                mensagem = "O ID deve ser maior que zero."
            });
        }

        await using var conexao = new MySqlConnection(_connectionString);

        await conexao.OpenAsync();

        const string sql = """
            UPDATE produtos
            SET Ativo = @ativo
            WHERE Id = @id;
            """;

        await using var comando = new MySqlCommand(sql, conexao);

        comando.Parameters.AddWithValue("@id", id);
        comando.Parameters.AddWithValue("@ativo", dados.Ativo!.Value);

        var linhasEncontradas = await comando.ExecuteNonQueryAsync();

        if (linhasEncontradas == 0)
        {
            await using var consulta = new MySqlCommand(
                "SELECT COUNT(*) FROM produtos WHERE Id = @id;",
                conexao);

            consulta.Parameters.AddWithValue("@id", id);

            var quantidade = Convert.ToInt32(
                await consulta.ExecuteScalarAsync());

            if (quantidade == 0)
            {
                return NotFound(new
                {
                    mensagem = "Produto não encontrado."
                });
            }
        }

        return NoContent();
    }
    [Authorize(Roles = "Administrador")]
    [HttpPatch("/api/admin/produtos/{id:int}/estoque")]
    public async Task<IActionResult> AjustarEstoque(
        int id,
        [FromBody] AjustarEstoqueDto dados)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                mensagem = "O ID deve ser maior que zero."
            });
        }

        var ajuste = dados.Ajuste!.Value;

        if (ajuste == 0)
        {
            return BadRequest(new
            {
                mensagem = "O ajuste deve ser diferente de zero."
            });
        }

        await using var conexao = new MySqlConnection(_connectionString);
        await conexao.OpenAsync();

        const string sql = """
            UPDATE produtos
            SET Estoque = Estoque + @ajuste
            WHERE Id = @id
            AND Estoque + @ajuste BETWEEN 0 AND 2147483647;
            """;

        await using var comando = new MySqlCommand(sql, conexao);

        comando.Parameters.AddWithValue("@id", id);
        comando.Parameters.AddWithValue("@ajuste", (long)ajuste);

        var linhas = await comando.ExecuteNonQueryAsync();

        if (linhas == 0)
        {
            await using var consulta = new MySqlCommand(
                "SELECT COUNT(*) FROM produtos WHERE Id = @id;",
                conexao);

            consulta.Parameters.AddWithValue("@id", id);

            var quantidade = Convert.ToInt32(
                await consulta.ExecuteScalarAsync());

            if (quantidade == 0)
            {
                return NotFound(new
                {
                    mensagem = "Produto não encontrado."
                });
            }

            return Conflict(new
            {
                mensagem = "O ajuste deixaria o estoque fora do limite permitido."
            });
        }

        return NoContent();
    }
    [HttpGet("sincronizacao")]
    public async Task<ActionResult<List<Produto>>> Sincronizar()
    {
        return Ok(await ConsultarCatalogoCompletoAsync());
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet("/api/admin/produtos")]
    public async Task<ActionResult<List<Produto>>> ListarParaLoja()
    {
        return Ok(await ConsultarCatalogoCompletoAsync());
    }

    private async Task<List<Produto>> ConsultarCatalogoCompletoAsync()
    {
        var produtos = new List<Produto>();

        await using var conexao = new MySqlConnection(_connectionString);
        await conexao.OpenAsync();

        const string sql = """
            SELECT Id, Nome, Categoria, Alcoolico,
                Descricao, Preco, Estoque, Ativo
            FROM produtos
            ORDER BY Nome;
            """;

        await using var comando = new MySqlCommand(sql, conexao);
        await using var leitor = await comando.ExecuteReaderAsync();

        while (await leitor.ReadAsync())
        {
            produtos.Add(new Produto
            {
                Id = leitor.GetInt32("Id"),
                Nome = leitor.GetString("Nome"),
                Categoria = leitor.GetString("Categoria"),
                Alcoolico = leitor.GetBoolean("Alcoolico"),
                Descricao = leitor.GetString("Descricao"),
                Preco = leitor.GetDecimal("Preco"),
                Estoque = leitor.GetInt32("Estoque"),
                Ativo = leitor.GetBoolean("Ativo")
            });
        }

        return produtos;
    }
}