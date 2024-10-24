using System.Diagnostics.CodeAnalysis;

namespace Nullinside.Api.Common.Exceptions;

/// <summary>
///   An exception thrown if an action continues to fail after retrying.
/// </summary>
[ExcludeFromCodeCoverage]
public class RetryException : Exception {
  /// <summary>
  ///   Initializes a new instance of the <see cref="RetryException" /> class.
  /// </summary>
  public RetryException() {
  }

  /// <summary>
  ///   Initializes a new instance of the <see cref="RetryException" /> class.
  /// </summary>
  /// <param name="message">The exception message.</param>
  public RetryException(string message) : base(message) {
  }

  /// <summary>
  ///   Initializes a new instance of the <see cref="RetryException" /> class.
  /// </summary>
  /// <param name="message">The exception message.</param>
  /// <param name="inner">The inner exception.</param>
  public RetryException(string message, Exception inner) : base(message, inner) {
  }

  /// <summary>
  ///   Initializes a new instance of the <see cref="RetryException" /> class.
  /// </summary>
  /// <param name="thrownExceptions">The exceptions thrown when retrying the method, in the order they are thrown.</param>
  public RetryException(IEnumerable<Exception> thrownExceptions) {
    ThrownExceptions = thrownExceptions;
  }

  /// <summary>
  ///   The exceptions thrown when retrying the method, in the order they are thrown.
  /// </summary>
  public IEnumerable<Exception>? ThrownExceptions { get; }
}