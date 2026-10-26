using System.Linq.Expressions;
namespace PetJourneyTutorApi.Persistence;
public interface IRepository<T> where T : class
{
    IQueryable<T> Query { get; }
    Task<List<TResult>> ListAsync<TResult>(IQueryable<TResult> query, CancellationToken ct = default);
    Task<int> CountAsync(IQueryable<T> query, CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<T?> FirstAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    void Add(T entity);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
}
public interface IDataSession
{
    IRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<IDataTransaction> BeginTransactionAsync(CancellationToken ct = default);
}
public interface IDataTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
}
