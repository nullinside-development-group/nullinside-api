using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Nullinside.Api.Shared;

/// <summary>
///   A contract for a service that persists web sockets so they can be used for asynchronous communication over long
///   periods of time.
/// </summary>
public interface IWebSocketPersister {
  /// <summary>
  ///   A collection of web sockets key'd by an identifier for the web socket connection.
  /// </summary>
  ConcurrentDictionary<string, WebSocket> WebSockets { get; set; }
}