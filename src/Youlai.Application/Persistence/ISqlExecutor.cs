using System.Data.Common;

namespace Youlai.Application.Persistence;

/// <summary>
/// 原生 SQL 执行。获取/关闭 DbConnection，用于直接执行 SQL
/// </summary>
public interface ISqlExecutor
{
    /// <summary>取得连接，未打开则先异步打开</summary>
    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>关闭连接</summary>
    Task CloseConnectionAsync(DbConnection connection, CancellationToken cancellationToken = default);
}
