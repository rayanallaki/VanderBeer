using Loja.Api.Dtos;
using MySqlConnector;
namespace Loja.Api.Services;

public class PedidoService
{
    private readonly string _connectionString;

    public PedidoService(IConfiguration configuracao)
    {
        _connectionString = configuracao.GetConnectionString("MySql")
            ?? throw new InvalidOperationException(
                "A conexão MySql não foi configurada.");
    }
    
    public List<CriarItemPedidoDto> PrepararItens(CriarPedidoDto dados)
    {
        if (dados.ChaveRequisicao is null ||
            dados.ChaveRequisicao == Guid.Empty)
        {
            throw new ArgumentException(
                "Informe uma chave válida para a tentativa de compra.");
        }

        if (dados.Itens is null ||
            dados.Itens.Count == 0 ||
            dados.Itens.Count > 100)
        {
            throw new ArgumentException(
                "O pedido deve conter entre 1 e 100 itens.");
        }

        var itensPreparados = new List<CriarItemPedidoDto>();

        foreach (var grupo in dados.Itens.GroupBy(item =>
        {
            if (item is null)
            {
                throw new ArgumentException(
                    "O pedido contém um item inválido.");
            }

            return item.ProdutoId;
        }))
        {
            var primeiro = grupo.First();

            if (primeiro.ProdutoId is null ||
                primeiro.ProdutoId.Value <= 0)
            {
                throw new ArgumentException("Informe um produto válido.");
            }

            var precoEsperado = primeiro.PrecoEsperado;

            if (precoEsperado is null ||
                precoEsperado.Value < 0 ||
                precoEsperado.Value > 99999999.99m ||
                decimal.Round(precoEsperado.Value, 2) != precoEsperado.Value)
            {
                throw new ArgumentException(
                    "Informe um preço válido com até duas casas decimais.");
            }

            long quantidadeTotal = 0;

            foreach (var item in grupo)
            {
                if (item.Quantidade is null || item.Quantidade.Value <= 0)
                {
                    throw new ArgumentException(
                        "A quantidade deve ser maior que zero.");
                }

                if (item.PrecoEsperado != precoEsperado)
                {
                    throw new ArgumentException(
                        "O mesmo produto foi enviado com preços diferentes.");
                }

                quantidadeTotal += item.Quantidade.Value;
            }

            if (quantidadeTotal > int.MaxValue)
            {
                throw new ArgumentException(
                    "A quantidade total do produto ultrapassa o limite permitido.");
            }

            itensPreparados.Add(new CriarItemPedidoDto
            {
                ProdutoId = primeiro.ProdutoId,
                Quantidade = (int)quantidadeTotal,
                PrecoEsperado = precoEsperado
            });
        }

        return itensPreparados
            .OrderBy(item => item.ProdutoId)
            .ToList();
    }
    private async Task<List<ItemPedidoRespostaDto>> ConferirProdutosAsync(
    List<CriarItemPedidoDto> itens,
    MySqlConnection conexao,
    MySqlTransaction transacao)
    {
        var resultado = new List<ItemPedidoRespostaDto>();
                 
        foreach (var item in itens)
        {
            const string sql = """
                SELECT Preco, Estoque, Ativo
                FROM produtos
                WHERE Id = @id
                FOR UPDATE;
                """;

            await using var comando = new MySqlCommand(
                sql,
                conexao,
                transacao);

            comando.Parameters.AddWithValue("@id", item.ProdutoId!.Value);

            await using var leitor = await comando.ExecuteReaderAsync();

            if (!await leitor.ReadAsync())
            {
                throw new InvalidOperationException(
                    $"O produto {item.ProdutoId} não foi encontrado.");
            }

            var preco = leitor.GetDecimal("Preco");
            var estoque = leitor.GetInt32("Estoque");
            var ativo = leitor.GetBoolean("Ativo");
            var quantidade = item.Quantidade!.Value;

            if (!ativo)
            {
                throw new InvalidOperationException(
                    $"O produto {item.ProdutoId} está indisponível.");
            }

            if (preco != item.PrecoEsperado!.Value)
            {
                throw new InvalidOperationException(
                    $"O preço do produto {item.ProdutoId} mudou. " +
                    "Atualize o carrinho antes de confirmar.");
            }

            if (estoque < quantidade)
            {
                throw new InvalidOperationException(
                    $"Estoque insuficiente para o produto {item.ProdutoId}.");
            }

            var subtotal = preco * quantidade;

            if (subtotal > 9999999999.99m)
            {
                throw new ArgumentException(
                    "O valor do item ultrapassa o limite permitido.");
            }

            resultado.Add(new ItemPedidoRespostaDto
            {
                ProdutoId = item.ProdutoId.Value,
                Quantidade = quantidade,
                PrecoUnitario = preco,
                Subtotal = subtotal
            });
        }

        return resultado;
    }
    private async Task<PedidoRespostaDto?> BuscarPedidoExistenteAsync(
    int usuarioId,
    string chaveRequisicao,
    MySqlConnection conexao,
    MySqlTransaction transacao)
    {
        const string sqlPedido = """
            SELECT Id, DataHora, Status, Observacao, PrecoTotal
            FROM pedidos
            WHERE UsuarioId = @usuarioId
            AND ChaveRequisicao = @chave
            FOR UPDATE;
            """;

        await using var comandoPedido = new MySqlCommand(
            sqlPedido,
            conexao,
            transacao);

        comandoPedido.Parameters.AddWithValue("@usuarioId", usuarioId);
        comandoPedido.Parameters.AddWithValue("@chave", chaveRequisicao);

        PedidoRespostaDto pedido;

        await using (var leitor = await comandoPedido.ExecuteReaderAsync())
        {
            if (!await leitor.ReadAsync())
            {
                return null;
            }

            pedido = new PedidoRespostaDto
            {
                Id = leitor.GetInt32("Id"),
                DataHora = DateTime.SpecifyKind(
                    leitor.GetDateTime("DataHora"),
                    DateTimeKind.Utc),
                Status = leitor.GetString("Status"),
                Observacao = leitor.GetString("Observacao"),
                PrecoTotal = leitor.GetDecimal("PrecoTotal")
            };
        }

        const string sqlItens = """
            SELECT ProdutoId, Quantidade, PrecoUnitario, Subtotal
            FROM itens_pedido
            WHERE PedidoId = @pedidoId
            ORDER BY ProdutoId;
            """;

        await using var comandoItens = new MySqlCommand(
            sqlItens,
            conexao,
            transacao);

        comandoItens.Parameters.AddWithValue("@pedidoId", pedido.Id);

        await using var leitorItens = await comandoItens.ExecuteReaderAsync();

        while (await leitorItens.ReadAsync())
        {
            pedido.Itens.Add(new ItemPedidoRespostaDto
            {
                ProdutoId = leitorItens.GetInt32("ProdutoId"),
                Quantidade = leitorItens.GetInt32("Quantidade"),
                PrecoUnitario = leitorItens.GetDecimal("PrecoUnitario"),
                Subtotal = leitorItens.GetDecimal("Subtotal")
            });
        }

        return pedido;
    }
    public async Task<PedidoRespostaDto> CriarAsync(
    int usuarioId,
    CriarPedidoDto dados)
    {
        var itens = PrepararItens(dados);
        var chave = dados.ChaveRequisicao!.Value.ToString("D");
        var observacao = dados.Observacao?.Trim() ?? string.Empty;

        if (observacao.Length > 300)
        {
            throw new ArgumentException(
                "A observação deve ter até 300 caracteres.");
        }

        await using var conexao = new MySqlConnection(_connectionString);

        await conexao.OpenAsync();

        await using var transacao = await conexao.BeginTransactionAsync();

        // Serializa as compras do mesmo usuário, inclusive reenvios simultâneos.
        await using (var comandoUsuario = new MySqlCommand(
            "SELECT Id FROM usuarios WHERE Id = @id FOR UPDATE;",
            conexao,
            transacao))
        {
            comandoUsuario.Parameters.AddWithValue("@id", usuarioId);

            var usuarioExistente = await comandoUsuario.ExecuteScalarAsync();

            if (usuarioExistente is null)
            {
                throw new InvalidOperationException(
                    "O usuário não foi encontrado.");
            }
        }

        var existente = await BuscarPedidoExistenteAsync(
            usuarioId,
            chave,
            conexao,
            transacao);

        if (existente is not null)
        {
            var mesmosItens = existente.Itens.Count == itens.Count;

            if (mesmosItens)
            {
                for (var i = 0; i < itens.Count; i++)
                {
                    var recebido = itens[i];
                    var salvo = existente.Itens[i];

                    if (recebido.ProdutoId != salvo.ProdutoId ||
                        recebido.Quantidade != salvo.Quantidade ||
                        recebido.PrecoEsperado != salvo.PrecoUnitario)
                    {
                        mesmosItens = false;
                        break;
                    }
                }
            }

            if (!mesmosItens || existente.Observacao != observacao)
            {
                throw new InvalidOperationException(
                    "Esta chave já foi utilizada em uma compra diferente.");
            }

            await transacao.CommitAsync();

            return existente;
        }

        var itensConferidos = await ConferirProdutosAsync(
            itens,
            conexao,
            transacao);

        var total = itensConferidos.Sum(item => item.Subtotal);

        if (total > 9999999999.99m)
        {
            throw new ArgumentException(
                "O total do pedido ultrapassa o limite permitido.");
        }

        var pedido = new PedidoRespostaDto
        {
            DataHora = DateTime.UtcNow,
            Status = "Recebido",
            Observacao = observacao,
            PrecoTotal = total,
            Itens = itensConferidos
        };

        const string sqlPedido = """
            INSERT INTO pedidos
                (UsuarioId, DataHora, PrecoTotal, Status,
                Observacao, ChaveRequisicao)
            VALUES
                (@usuarioId, @dataHora, @total, @status,
                @observacao, @chave);
            """;

        await using (var comandoPedido = new MySqlCommand(
            sqlPedido,
            conexao,
            transacao))
        {
            comandoPedido.Parameters.AddWithValue("@usuarioId", usuarioId);
            comandoPedido.Parameters.AddWithValue("@dataHora", pedido.DataHora);
            comandoPedido.Parameters.AddWithValue("@total", pedido.PrecoTotal);
            comandoPedido.Parameters.AddWithValue("@status", pedido.Status);
            comandoPedido.Parameters.AddWithValue("@observacao", observacao);
            comandoPedido.Parameters.AddWithValue("@chave", chave);

            await comandoPedido.ExecuteNonQueryAsync();

            pedido.Id = checked((int)comandoPedido.LastInsertedId);
        }

        foreach (var item in itensConferidos)
        {
            const string sqlItem = """
                INSERT INTO itens_pedido
                    (PedidoId, ProdutoId, Quantidade, PrecoUnitario, Subtotal)
                VALUES
                    (@pedidoId, @produtoId, @quantidade, @preco, @subtotal);
                """;

            await using (var comandoItem = new MySqlCommand(
                sqlItem,
                conexao,
                transacao))
            {
                comandoItem.Parameters.AddWithValue("@pedidoId", pedido.Id);
                comandoItem.Parameters.AddWithValue("@produtoId", item.ProdutoId);
                comandoItem.Parameters.AddWithValue("@quantidade", item.Quantidade);
                comandoItem.Parameters.AddWithValue("@preco", item.PrecoUnitario);
                comandoItem.Parameters.AddWithValue("@subtotal", item.Subtotal);

                await comandoItem.ExecuteNonQueryAsync();
            }

            const string sqlEstoque = """
                UPDATE produtos
                SET Estoque = Estoque - @quantidade
                WHERE Id = @produtoId
                AND Ativo = TRUE
                AND Estoque >= @quantidade;
                """;

            await using var comandoEstoque = new MySqlCommand(
                sqlEstoque,
                conexao,
                transacao);

            comandoEstoque.Parameters.AddWithValue("@produtoId", item.ProdutoId);
            comandoEstoque.Parameters.AddWithValue("@quantidade", item.Quantidade);

            var linhas = await comandoEstoque.ExecuteNonQueryAsync();

            if (linhas != 1)
            {
                throw new InvalidOperationException(
                    "Não foi possível atualizar o estoque.");
            }
        }

        await transacao.CommitAsync();

        return pedido;
    }
    private async Task<List<PedidoRespostaDto>> ConsultarPedidosAsync(
    int? usuarioId,
    string? status,
    DateTime? inicio,
    DateTime? fim,
    int? pedidoId = null)
    {
        await using var conexao = new MySqlConnection(_connectionString);

        await conexao.OpenAsync();

        const string sql = """
        SELECT
            p.Id,
            p.DataHora,
            p.Status,
            p.Observacao,
            p.PrecoTotal,
            i.ProdutoId,
            i.Quantidade,
            i.PrecoUnitario,
            i.Subtotal
        FROM pedidos p
        INNER JOIN itens_pedido i ON i.PedidoId = p.Id
        WHERE (@usuarioId IS NULL OR p.UsuarioId = @usuarioId)
        AND (@status IS NULL OR p.Status = @status)
        AND (@inicio IS NULL OR p.DataHora >= @inicio)
        AND (@fim IS NULL OR p.DataHora < @fim)
        AND (@pedidoId IS NULL OR p.Id = @pedidoId)
        ORDER BY p.DataHora DESC, p.Id DESC, i.ProdutoId;
        """;

        await using var comando = new MySqlCommand(sql, conexao);

        comando.Parameters.AddWithValue(
        "@usuarioId", (object?)usuarioId ?? DBNull.Value);

        comando.Parameters.AddWithValue(
        "@status", (object?)status ?? DBNull.Value);

        comando.Parameters.AddWithValue(
        "@inicio", (object?)inicio ?? DBNull.Value);

        comando.Parameters.AddWithValue(
        "@fim", (object?)fim ?? DBNull.Value);

        comando.Parameters.AddWithValue(
        "@pedidoId", (object?)pedidoId ?? DBNull.Value);

        await using var leitor = await comando.ExecuteReaderAsync();

        var pedidos = new List<PedidoRespostaDto>();
        var pedidosPorId = new Dictionary<int, PedidoRespostaDto>();

        while (await leitor.ReadAsync())
        {
           var idEncontrado = leitor.GetInt32("Id");

            if (!pedidosPorId.TryGetValue(idEncontrado, out var pedido))
            {
                pedido = new PedidoRespostaDto
                {
                    Id = idEncontrado,
                    DataHora = DateTime.SpecifyKind(
                        leitor.GetDateTime("DataHora"),
                        DateTimeKind.Utc),
                    Status = leitor.GetString("Status"),
                    Observacao = leitor.GetString("Observacao"),
                    PrecoTotal = leitor.GetDecimal("PrecoTotal")
                };
        
                pedidosPorId.Add(idEncontrado, pedido);
                pedidos.Add(pedido);
            }

            pedido.Itens.Add(new ItemPedidoRespostaDto
            {
                ProdutoId = leitor.GetInt32("ProdutoId"),
                Quantidade = leitor.GetInt32("Quantidade"),
                PrecoUnitario = leitor.GetDecimal("PrecoUnitario"),
                Subtotal = leitor.GetDecimal("Subtotal")
            });
        }

        return pedidos;
    }
    public async Task AtualizarStatusAsync(int pedidoId, string novoStatus)
    {
        var permitidos = new[]
        {
            "Recebido", "EmSeparacao", "Pronto", "Finalizado", "Cancelado"
        };

        if (!permitidos.Contains(novoStatus))
        {
            throw new ArgumentException("Status inválido.");
        }

        await using var conexao = new MySqlConnection(_connectionString);
        await conexao.OpenAsync();

        await using var transacao = await conexao.BeginTransactionAsync();

        string statusAtual;

        await using (var consulta = new MySqlCommand(
            "SELECT Status FROM pedidos WHERE Id = @id FOR UPDATE;",
            conexao,
            transacao))
        {
            consulta.Parameters.AddWithValue("@id", pedidoId);

            var resultado = await consulta.ExecuteScalarAsync();

            if (resultado is null)
            {
                throw new KeyNotFoundException("Pedido não encontrado.");
            }

            statusAtual = resultado.ToString()!;
        }

        // Repetir o mesmo status não deve produzir novos efeitos.
        if (statusAtual == novoStatus)
        {
            await transacao.CommitAsync();
            return;
        }

        var transicaoPermitida = (statusAtual, novoStatus) switch
        {
            ("Recebido", "EmSeparacao") => true,
            ("Recebido", "Cancelado") => true,
            ("EmSeparacao", "Pronto") => true,
            ("EmSeparacao", "Cancelado") => true,
            ("Pronto", "Finalizado") => true,
            _ => false
        };

        if (!transicaoPermitida)
        {
            throw new InvalidOperationException(
                $"Não é permitido alterar de {statusAtual} para {novoStatus}.");
        }

        if (novoStatus == "Cancelado")
        {
            var itens = new List<(int ProdutoId, int Quantidade)>();

            await using (var consultaItens = new MySqlCommand(
                """
                SELECT ProdutoId, Quantidade
                FROM itens_pedido
                WHERE PedidoId = @id
                ORDER BY ProdutoId;
                """,
                conexao,
                transacao))
            {
                consultaItens.Parameters.AddWithValue("@id", pedidoId);

                await using var leitor = await consultaItens.ExecuteReaderAsync();

                while (await leitor.ReadAsync())
                {
                    itens.Add((
                        leitor.GetInt32("ProdutoId"),
                        leitor.GetInt32("Quantidade")));
                }
            }

            foreach (var item in itens)
            {
                await using var devolver = new MySqlCommand(
                    """
                    UPDATE produtos
                    SET Estoque = Estoque + @quantidade
                    WHERE Id = @produtoId
                    AND Estoque <= @limite;
                    """,
                    conexao,
                    transacao);

                devolver.Parameters.AddWithValue("@produtoId", item.ProdutoId);
                devolver.Parameters.AddWithValue("@quantidade", item.Quantidade);
                devolver.Parameters.AddWithValue(
                    "@limite",
                    int.MaxValue - item.Quantidade);

                if (await devolver.ExecuteNonQueryAsync() != 1)
                {
                    throw new InvalidOperationException(
                        "Não foi possível devolver o estoque do produto.");
                }
            }
        }

        await using var atualizar = new MySqlCommand(
            "UPDATE pedidos SET Status = @status WHERE Id = @id;",
            conexao,
            transacao);

        atualizar.Parameters.AddWithValue("@id", pedidoId);
        atualizar.Parameters.AddWithValue("@status", novoStatus);

        await atualizar.ExecuteNonQueryAsync();
        await transacao.CommitAsync();
    }
    public Task<List<PedidoRespostaDto>> ListarDoUsuarioAsync(int usuarioId)
    {
        return ConsultarPedidosAsync(usuarioId, null, null, null);
    }

    public Task<List<PedidoRespostaDto>> ListarParaLojaAsync(
        string? status,
        DateTime? inicio,
        DateTime? fim)
    {
        return ConsultarPedidosAsync(null, status, inicio, fim);
    }
    public async Task<PedidoRespostaDto?> BuscarPorIdAsync(
    int pedidoId,
    int? usuarioId)
    {
        var pedidos = await ConsultarPedidosAsync(
            usuarioId,
            null,
            null,
            null,
            pedidoId);

        return pedidos.FirstOrDefault();
    }
}