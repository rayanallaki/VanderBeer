using Loja.Api.Dtos;
using Loja.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace Loja.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly string _connectionString;

    public UsuariosController(IConfiguration configuracao)
    {
        _connectionString = configuracao.GetConnectionString("MySql")
            ?? throw new InvalidOperationException(
                "A conexão MySql não foi configurada.");
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioRespostaDto>> Cadastrar(
        [FromBody] CriarUsuarioDto dados)
    {
        var documento = dados.Documento
            .Replace(".", "")
            .Replace("-", "")
            .Replace("/", "")
            .Replace(" ", "")
            .Trim()
            .ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(documento))
        {
            return BadRequest(new
            {
                mensagem = "Informe um documento válido."
            });
        }

        var usuario = new Usuario
        {
            Nome = dados.Nome.Trim(),
            Documento = documento,
            Rua = dados.Rua.Trim(),
            Bairro = dados.Bairro.Trim(),
            Cep = dados.Cep.Replace("-", ""),
            Numero = dados.Numero.Trim(),
            Complemento = dados.Complemento?.Trim() ?? string.Empty,
            Telefone = dados.Telefone.Trim(),
            Email = dados.Email.Trim().ToLowerInvariant(),
            Perfil = "Cliente"
        };

        var geradorHash = new PasswordHasher<Usuario>();

        usuario.SenhaHash = geradorHash.HashPassword(
            usuario,
            dados.Senha);

        await using var conexao = new MySqlConnection(_connectionString);

        await conexao.OpenAsync();

        const string sql = """
            INSERT INTO usuarios
                (Nome, Documento, Rua, Bairro, Cep, Numero,
                 Complemento, Telefone, Email, SenhaHash, Perfil)
            VALUES
                (@nome, @documento, @rua, @bairro, @cep, @numero,
                 @complemento, @telefone, @email, @senhaHash, @perfil);
            """;

        await using var comando = new MySqlCommand(sql, conexao);

        comando.Parameters.AddWithValue("@nome", usuario.Nome);
        comando.Parameters.AddWithValue("@documento", usuario.Documento);
        comando.Parameters.AddWithValue("@rua", usuario.Rua);
        comando.Parameters.AddWithValue("@bairro", usuario.Bairro);
        comando.Parameters.AddWithValue("@cep", usuario.Cep);
        comando.Parameters.AddWithValue("@numero", usuario.Numero);
        comando.Parameters.AddWithValue("@complemento", usuario.Complemento);
        comando.Parameters.AddWithValue("@telefone", usuario.Telefone);
        comando.Parameters.AddWithValue("@email", usuario.Email);
        comando.Parameters.AddWithValue("@senhaHash", usuario.SenhaHash);
        comando.Parameters.AddWithValue("@perfil", usuario.Perfil);

        try
        {
            await comando.ExecuteNonQueryAsync();
        }
        catch (MySqlException erro) when (erro.Number == 1062)
        {
            return Conflict(new
            {
                mensagem = "Já existe um cadastro com esse e-mail ou documento."
            });
        }

        var resposta = new UsuarioRespostaDto
        {
            Id = checked((int)comando.LastInsertedId),
            Nome = usuario.Nome,
            Email = usuario.Email
        };

        return StatusCode(StatusCodes.Status201Created, resposta);
    }
}