namespace PAMS.Domain.Common;

/// <summary>
/// Abstraction over DbContext.SaveChangesAsync for transaction coordination.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
