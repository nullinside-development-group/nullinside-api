using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Model;

namespace Nullinside.Api.Common;

/// <summary>
/// Handles acquiring a lock at the database level.
/// </summary>
/// <remarks>Requires a transaction wrapping its functionality.</remarks>
public class DatabaseLock : IDisposable {
  /// <summary>
  /// The database context.
  /// </summary>
  private INullinsideContext _mysqlDbContext;
  /// <summary>
  /// The lock name used on the previous lock.
  /// </summary>
  private string? _name;
  
  /// <summary>
  /// Initializes a new instance of the <see cref="DatabaseLock"/> class.
  /// </summary>
  /// <param name="mysqlDbContext">The database context.</param>
  /// <exception cref="ArgumentNullException">Must provide parameter.</exception>
  public DatabaseLock(INullinsideContext mysqlDbContext) {
    _mysqlDbContext = mysqlDbContext ?? throw new ArgumentNullException(nameof(mysqlDbContext));
  }

  /// <summary>
  /// Acquires the lock.
  /// </summary>
  /// <remarks>Waits indefinitely.</remarks>
  /// <param name="name">The name of the lock. MUST BE A HARD CODED VALUE</param>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>True if successful, false otherwise.</returns>
  public async Task<bool> GetLock(string name, CancellationToken cancellationToken = new()) {
    if (string.IsNullOrWhiteSpace(name)) {
      return false;
    }
    
    // This is only used with hard coded names.
#pragma warning disable EF1002
    await _mysqlDbContext.Database.ExecuteSqlRawAsync($"SELECT GET_LOCK('{name}', -1)", cancellationToken: cancellationToken);
#pragma warning restore EF1002
    _name = name;
    return true;
  }
  
  /// <summary>
  /// Releases the lock.
  /// </summary>
  /// <param name="name">The name of the lock. MUST BE A HARD CODED VALUE</param>
  /// <param name="cancellationToken">The cancellation token.</param>
  public async Task ReleaseLock(string name, CancellationToken cancellationToken = new()) {
    if (!string.IsNullOrWhiteSpace(name) && !name.Equals(_name, StringComparison.InvariantCultureIgnoreCase)) {
      // This is only used with hard coded names.
#pragma warning disable EF1002
      await _mysqlDbContext.Database.ExecuteSqlRawAsync($"SELECT RELEASE_LOCK('{_name}')", cancellationToken: cancellationToken);
#pragma warning restore EF1002
    }
    
    // This is only used with hard coded names.
#pragma warning disable EF1002
    await _mysqlDbContext.Database.ExecuteSqlRawAsync($"SELECT RELEASE_LOCK('{name}')", cancellationToken: cancellationToken);
#pragma warning restore EF1002
    _name = null;
  }

  /// <summary>
  /// Disposes of resources.
  /// </summary>
  /// <param name="disposing">True if dispose called, false if finalizer.</param>
  protected virtual void Dispose(bool disposing) {
    if (disposing) {
      if (!string.IsNullOrWhiteSpace(_name)) {
        Task.WaitAll(this.ReleaseLock(_name));
      }
    }
  }

  /// <inheritdoc />
  public void Dispose() {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// Finalizes instance of the <see cref="DatabaseLock"/> class.
  /// </summary>
  ~DatabaseLock() {
    Dispose(false);
  }
}