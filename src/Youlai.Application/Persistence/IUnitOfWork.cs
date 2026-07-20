namespace Youlai.Application.Persistence;

/// <summary>
/// 工作单元端口：由业务显式控制落库时机与事务边界
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 事务端口：由业务通过 await using 控制提交或回滚
/// </summary>
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
