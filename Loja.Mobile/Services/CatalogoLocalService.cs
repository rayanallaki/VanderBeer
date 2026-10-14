using Loja.Mobile.Models;
using SQLite;

namespace Loja.Mobile.Services;

public class CatalogoLocalService
{
    public static CatalogoLocalService Instancia { get; } = new();

    private readonly SQLiteAsyncConnection _banco;
    private readonly Task _inicializacao;

    private CatalogoLocalService()
    {
        var caminho = Path.Combine(
            FileSystem.AppDataDirectory,
            "vanderbeer.db3");

        _banco = new SQLiteAsyncConnection(caminho);
        _inicializacao = InicializarAsync();
    }

    private async Task InicializarAsync()
    {
        await _banco.CreateTableAsync<Produto>();
        await _banco.CreateTableAsync<EstadoCatalogo>();
    }

    public async Task<List<Produto>> ListarAtivosAsync()
    {
        await _inicializacao;

        return await _banco.Table<Produto>()
            .Where(produto => produto.Ativo)
            .OrderBy(produto => produto.Nome)
            .ToListAsync();
    }

    public async Task<DateTime?> ObterUltimaSincronizacaoAsync()
    {
        await _inicializacao;

        var estado = await _banco.FindAsync<EstadoCatalogo>(1);

        return estado?.UltimaSincronizacaoUtc;
    }

    public async Task SalvarCatalogoAsync(List<Produto> produtos)
    {
        await _inicializacao;

        var estado = new EstadoCatalogo
        {
            Id = 1,
            UltimaSincronizacaoUtc = DateTime.UtcNow
        };

        await _banco.RunInTransactionAsync(conexao =>
        {
            conexao.DeleteAll<Produto>();
            conexao.InsertAll(produtos);
            conexao.InsertOrReplace(estado);
        });
    }
    public async Task<Produto?> ObterProdutoAsync(int id)
    {
        await _inicializacao;
        return await _banco.FindAsync<Produto>(id);
    }
}

public class EstadoCatalogo
{
    [PrimaryKey]
    public int Id { get; set; }

    public DateTime UltimaSincronizacaoUtc { get; set; }
}
