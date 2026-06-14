using System.Collections.Concurrent;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

using log4net;

namespace Nullinside.Api.Common.Twitch;

/// <summary>
///   A client for connecting to a twitch IRC server.
/// </summary>
public sealed class TwitchIrcClient : IDisposable {
  /// <summary>
  ///   The logger.
  /// </summary>
  private static readonly ILog LOG = LogManager.GetLogger(typeof(TwitchIrcClient));

  /// <summary>
  ///   The channels we've connected to.
  /// </summary>
  private readonly ConcurrentDictionary<string, string> _channels = new();

  /// <summary>
  ///   The lock used to ensure only one connection attempt happens at a time.
  /// </summary>
  private readonly SemaphoreSlim _connectionLock = new(1, 1);

  /// <summary>
  ///   The oauth token to use to connect.
  /// </summary>
  private string _oauthToken;

  /// <summary>
  ///   The stream reader used to communicate with the IRC server.
  /// </summary>
  private StreamReader? _reader;

  /// <summary>
  ///   The SSL stream used to communicate with the IRC server.
  /// </summary>
  private SslStream? _sslStream;

  /// <summary>
  ///   The TCP client used to communicate with the IRC server.
  /// </summary>
  private TcpClient? _tcpClient;

  /// <summary>
  ///   The twitch username to connect as.
  /// </summary>
  private string _username;

  /// <summary>
  ///   The stream writer used to send messages to the IRC server.
  /// </summary>
  private StreamWriter? _writer;

  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchIrcClient" /> class.
  /// </summary>
  /// <param name="username">The twitch username to connect as.</param>
  /// <param name="oauthToken">The oauth token to use to connect.</param>
  public TwitchIrcClient(string username, string oauthToken) {
    _username = username;
    _oauthToken = NormalizeToken(oauthToken);
  }

  /// <inheritdoc />
  public void Dispose() {
    _connectionLock.Dispose();

    _writer?.Dispose();
    _reader?.Dispose();
    _sslStream?.Dispose();
    _tcpClient?.Dispose();
  }

  /// <summary>
  ///   Connects to the twitch IRC server.
  /// </summary>
  public async Task ConnectAsync() {
    await _connectionLock.WaitAsync().ConfigureAwait(false);

    try {
      await DisconnectInternalAsync().ConfigureAwait(false);
      await ConnectInternalAsync().ConfigureAwait(false);
    }
    finally {
      _connectionLock.Release();
    }
  }
  
  /// <summary>
  ///   Disconnects from the twitch IRC server.
  /// </summary>
  public async Task DisconnectAsync() {
    await _connectionLock.WaitAsync().ConfigureAwait(false);

    try {
      await DisconnectInternalAsync().ConfigureAwait(false);
    }
    finally {
      _connectionLock.Release();
    }
  }

  /// <summary>
  ///   Updates the credentials used to connect to the IRC server.
  /// </summary>
  /// <param name="username">The new username.</param>
  /// <param name="oauthToken">The new oauth token.</param>
  public async Task UpdateCredentialsAsync(string username, string oauthToken) {
    await _connectionLock.WaitAsync().ConfigureAwait(false);

    try {
      _username = username;
      _oauthToken = NormalizeToken(oauthToken);

      await DisconnectInternalAsync().ConfigureAwait(false);
      await ConnectInternalAsync().ConfigureAwait(false);
    }
    finally {
      _connectionLock.Release();
    }
  }

  /// <summary>
  ///   Sends a message to the twitch irc channel.
  /// </summary>
  /// <param name="channel">The channel to send the message to.</param>
  /// <param name="message">The message to send.</param>
  /// <exception cref="InvalidOperationException">Not connected, call <see cref="ConnectAsync" /> with valid credentials.</exception>
  public async Task SendMessageAsync(string channel, string message) {
    if (_writer == null) {
      throw new InvalidOperationException("Not connected.");
    }

    await _writer.WriteLineAsync($"PRIVMSG #{channel} :{message}").ConfigureAwait(false);
  }

  /// <summary>
  ///   Connects to a channel.
  /// </summary>
  /// <param name="channel">The channel to connect to.</param>
  public async Task AddChannelAsync(string channel) {
    if (_channels.TryAdd(channel, channel)) {
      await SendRawAsync($"JOIN #{channel}").ConfigureAwait(false);
    }
  }

  /// <summary>
  ///   Disconnects from a channel.
  /// </summary>
  /// <param name="channel">The channel to disconnect from.</param>
  public async Task RemoveChannelAsync(string channel) {
    if (_channels.TryRemove(channel, out _)) {
      await SendRawAsync($"PART #{channel}").ConfigureAwait(false);
    }
  }

  /// <summary>
  ///   Performs a blocking read loop to read messages from the IRC server.
  /// </summary>
  /// <param name="onMessage">The function to call when a message is received.</param>
  /// <param name="onBan">The function to call when a ban is received.</param>
  /// <param name="token">The cancellation token to use for the read loop.</param>
  public async Task ReadLoopAsync(Func<string, Task>? onMessage = null, Func<string, Task>? onBan = null, CancellationToken token = default) {
    while (!token.IsCancellationRequested) {
      StreamReader? reader = _reader;

      if (reader == null) {
        break;
      }

      string? line = await reader.ReadLineAsync().ConfigureAwait(false);
      if (line == null) {
        break;
      }

      LOG.Debug($"<< {line}");

      if (line.StartsWith("PING ")) {
        await SendRawAsync($"PONG {line["PING ".Length..]}").ConfigureAwait(false);
        continue;
      }

      if (line.Contains(":tmi.twitch.tv RECONNECT")) {
        await _connectionLock.WaitAsync().ConfigureAwait(false);

        try {
          await DisconnectInternalAsync().ConfigureAwait(false);
          await ConnectInternalAsync().ConfigureAwait(false);
        }
        finally {
          _connectionLock.Release();
        }

        continue;
      }

      if (null != onBan && line.Contains(" CLEARCHAT ") && line.Contains("target-user-id=")) {
        await onBan(line).ConfigureAwait(false);
        continue;
      }

      if (onMessage != null) {
        await onMessage(line).ConfigureAwait(false);
      }
    }
  }

  /// <summary>
  ///   Connects to the twitch IRC server and joins any channels specified in <see cref="_channels" />.
  /// </summary>
  private async Task ConnectInternalAsync() {
    _tcpClient = new TcpClient();
    await _tcpClient.ConnectAsync("irc.chat.twitch.tv", 6697).ConfigureAwait(false);

    _sslStream = new SslStream(_tcpClient.GetStream(), false);
    await _sslStream.AuthenticateAsClientAsync("irc.chat.twitch.tv").ConfigureAwait(false);

    _reader = new StreamReader(_sslStream, new UTF8Encoding(false));
    _writer = new StreamWriter(_sslStream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\r\n" };

    await SendRawAsync("CAP REQ :twitch.tv/tags twitch.tv/commands twitch.tv/membership").ConfigureAwait(false);
    await SendRawAsync($"PASS {_oauthToken}").ConfigureAwait(false);
    await SendRawAsync($"NICK {_username}").ConfigureAwait(false);

    foreach (string channel in _channels.Keys) {
      await SendRawAsync($"JOIN #{channel}").ConfigureAwait(false);
    }
  }

  /// <summary>
  ///   Disconnects from the twitch IRC server.
  /// </summary>
  private async Task DisconnectInternalAsync() {
    try {
      if (_writer != null) {
        await _writer.FlushAsync().ConfigureAwait(false);
      }
    }
    catch {
      // do nothing
    }

    _writer?.Dispose();
    _reader?.Dispose();
    _sslStream?.Dispose();
    _tcpClient?.Dispose();

    _writer = null;
    _reader = null;
    _sslStream = null;
    _tcpClient = null;
  }

  /// <summary>
  ///   Sends a raw IRC command to the server.
  /// </summary>
  /// <param name="command">The command.</param>
  /// <exception cref="InvalidOperationException">Not connected, connect with <see cref="ConnectAsync" />.</exception>
  private async Task SendRawAsync(string command) {
    if (_writer == null) {
      throw new InvalidOperationException("Not connected.");
    }

    LOG.Debug($">> {command}");

    await _writer.WriteLineAsync(command).ConfigureAwait(false);
  }

  /// <summary>
  ///   Normalizes the specification for oauth tokens.
  /// </summary>
  /// <param name="token">The token.</param>
  /// <returns>The normalized oauth token string.</returns>
  private static string NormalizeToken(string token) {
    return token.StartsWith("oauth:") ? token : $"oauth:{token}";
  }
}