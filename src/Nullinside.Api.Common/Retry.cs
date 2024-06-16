using Nullinside.Api.Common.Exceptions;

namespace Nullinside.Api.Common;

/// <summary>
///   A helper class for re-running methods more than once.
/// </summary>
public static class Retry {
  /// <summary>
  ///   Re-runs the provided action multiple times, stopping when it executes successfully or runs out of retries.
  /// </summary>
  /// <param name="action">The action to execute.</param>
  /// <param name="numberOfRetries">The number of times to retry, on failure, inclusive.</param>
  /// <param name="token">The cancellation.</param>
  /// <param name="waitTime">The, optional, time to wait between retries. If null, no wait.</param>
  /// <param name="runOnFailure">
  ///   The, optional, action to run after a failure has occurred. Runs AFTER the specified <paramref name="waitTime" />.
  /// </param>
  /// <remarks>
  ///   Success or failure of the execution of <paramref name="action" /> is entirely judged based on the presence or
  ///   absence of a throw exception.
  /// </remarks>
  /// <typeparam name="T">The return type of the <paramref name="action" />.</typeparam>
  /// <returns>Whatever <paramref name="action" /> returns, if successful. Throws <see cref="RetryException" /> on failures.</returns>
  /// <exception cref="RetryException">
  ///   <paramref name="action" /> was executed <paramref name="numberOfRetries" /> times and it never
  ///   succeeded.
  /// </exception>
  public static async Task<T> Execute<T>(Func<Task<T>> action, int numberOfRetries, CancellationToken token = new(),
    TimeSpan? waitTime = null, Action<Exception>? runOnFailure = null) {
    int tries = 0;
    Exception? exceptionToThrow = null;
    while (tries <= numberOfRetries && !token.IsCancellationRequested) {
      try {
        return await action();
      }
      catch (Exception ex) {
        exceptionToThrow = ex;
        ++tries;

        // Why did I do as delay THEN method? Honestly...I don't have a good reason. You can change it if you want, just
        // update the documentation if you do.
        await Task.Delay(waitTime.HasValue ? (int)waitTime.Value.TotalMilliseconds : 1000, token);
        if (null != runOnFailure) {
          runOnFailure(ex);
        }
      }
    }

    if (null != exceptionToThrow) {
      throw exceptionToThrow;
    }

    throw new RetryException($"Error after {tries - 1} tries");
  }
}