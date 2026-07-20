using Microsoft.EntityFrameworkCore;
using Youlai.Application.Persistence;

namespace Youlai.Infrastructure.Persistence;

/// <summary>
/// EF Core 工作单元（DbContext 由 DI 管理，此处不释放）
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _ctx;

    public UnitOfWork(AppDbContext ctx) => _ctx = ctx;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _ctx.SaveChangesAsync(cancellationToken);

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => new EfTransaction(await _ctx.Database.BeginTransactionAsync(cancellationToken));

    public void Dispose()
    {
        // DbContext 由 DI 管理，不在此释放
    }
}

/// <summary>
/// EF Core 事务
/// </summary>
internal sealed class EfTransaction : ITransaction
{
    private readonly Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction _tx;

    public EfTransaction(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx) => _tx = tx;

    public Task CommitAsync(CancellationToken cancellationToken = default) => _tx.CommitAsync(cancellationToken);
    public Task RollbackAsync(CancellationToken cancellationToken = default) => _tx.RollbackAsync(cancellationToken);
    public ValueTask DisposeAsync() => _tx.DisposeAsync();
}
