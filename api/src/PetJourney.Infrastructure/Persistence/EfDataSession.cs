using System.Linq.Expressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Oracle.ManagedDataAccess.Client;
using PetJourneyTutorApi.Data;
using PetJourneyTutorApi.Errors;
using PetJourneyTutorApi.Persistence;
namespace PetJourney.Infrastructure.Persistence;
public sealed class EfRepository<T>(AppDbContext context) : IRepository<T> where T : class
{
    public IQueryable<T> Query => context.Set<T>();
    public Task<List<TResult>> ListAsync<TResult>(IQueryable<TResult> query, CancellationToken ct = default) => query.ToListAsync(ct);
    public Task<int> CountAsync(IQueryable<T> query, CancellationToken ct = default) => query.CountAsync(ct);
    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) => Query.AnyAsync(predicate, ct);
    public Task<T?> FirstAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) => Query.FirstOrDefaultAsync(predicate, ct);
    public void Add(T entity) => context.Set<T>().Add(entity);
    public void Remove(T entity) => context.Set<T>().Remove(entity);
    public void RemoveRange(IEnumerable<T> entities) => context.Set<T>().RemoveRange(entities);
}
public sealed class EfDataSession(AppDbContext context) : IDataSession
{
    public IRepository<T> Repository<T>() where T : class => new EfRepository<T>(context);
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try { return await context.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: 19 } ||
            ex.InnerException is OracleException { Number: 1 or 2291 or 2292 })
        { throw new DataConflictException("A operação conflita com dados existentes ou relacionados.", ex); }
    }
    public async Task<IDataTransaction> BeginTransactionAsync(CancellationToken ct = default) =>
        new EfDataTransaction(await context.Database.BeginTransactionAsync(ct));
    private sealed class EfDataTransaction(IDbContextTransaction transaction) : IDataTransaction
    {
        public Task CommitAsync(CancellationToken ct = default) => transaction.CommitAsync(ct);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
