namespace Youlai.Application.Persistence;

/// <summary>
/// 统一落库与事务边界
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 数据库事务，通过 await using 控制提交/回滚
/// </summary>
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
