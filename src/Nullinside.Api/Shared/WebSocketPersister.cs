using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Nullinside.Api.Shared;

/// <summary>
///   Persists web sockets so they can be used for asynchronous communication over long periods of time.
/// </summary>
public class WebSocketPersister : IWebSocketPersister {
  /// <inheritdoc />
  public ConcurrentDictionary<string, WebSocket> WebSockets { get; set; } = new();
}