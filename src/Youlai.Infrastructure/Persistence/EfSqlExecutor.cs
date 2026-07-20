using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Youlai.Application.Persistence;

namespace Youlai.Infrastructure.Persistence;

/// <summary>
/// EF Core 原生 SQL 执行。连接来自 AppDbContext.Database
/// </summary>
internal sealed class EfSqlExecutor : ISqlExecutor
{
    private readonly AppDbContext _ctx;

    public EfSqlExecutor(AppDbContext ctx) => _ctx = ctx;

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = _ctx.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        return connection;
    }

    public Task CloseConnectionAsync(DbConnection connection, CancellationToken cancellationToken = default)
        => _ctx.Database.CloseConnectionAsync();
}
