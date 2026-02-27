using PAMS.Domain.Common;

namespace PAMS.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly PamsDbContext _context;

    public UnitOfWork(PamsDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
