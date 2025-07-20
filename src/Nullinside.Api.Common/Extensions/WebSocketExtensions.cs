using System.Net.WebSockets;
using System.Text;

namespace Nullinside.Api.Common.Extensions;

/// <summary>
///   Extensions for the <see cref="WebSocket" /> class.
/// </summary>
public static class WebSocketExtensions {
  /// <summary>
  ///   Sends text over a web socket.
  /// </summary>
  /// <param name="webSocket">The web socket to send the message over.</param>
  /// <param name="message">The message to send.</param>
  /// <param name="cancelToken">The cancellation token.</param>
  public static async Task SendTextAsync(this WebSocket webSocket, string message, CancellationToken cancelToken = new()) {
    await webSocket.SendAsync(Encoding.ASCII.GetBytes(message), WebSocketMessageType.Text, true, cancelToken);
  }

  /// <summary>
  ///   Receives text over a web socket.
  /// </summary>
  /// <param name="webSocket">The web socket to send the message over.</param>
  /// <param name="cancelToken">The cancellation token.</param>
  /// <returns></returns>
  public static async Task<string> ReceiveTextAsync(this WebSocket webSocket, CancellationToken cancelToken = new()) {
    WebSocketReceiveResult response;
    var fullMessage = new List<byte>();

    do {
      var data = new ArraySegment<byte>(new byte[1024]);
      response = await webSocket.ReceiveAsync(data, cancelToken);
      fullMessage.AddRange(data);
    } while (null == response.CloseStatus && !response.EndOfMessage);

    // Remove the null character from the end of the string, this happens when the buffer is only partially filled.
    return Encoding.ASCII.GetString(fullMessage.ToArray()).TrimEnd('\0');
  }
}