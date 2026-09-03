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
  ///   The lock used to ensure only one connection attempt happens at a time.
  /// </summary>
  private readonly SemaphoreSlim _connectionLock = new(1, 1);

  /// <summary>
  ///   The channels we want to be connected to.
  /// </summary>
  private readonly ConcurrentDictionary<string, string> _desiredChannels = new();

  /// <summary>
  ///   The channels we've confirmed we're currently joined to.
  /// </summary>
  private readonly ConcurrentDictionary<string, byte> _joinedChannels = new();

  /// <summary>
  ///   The lock used to ensure only one write happens at a time.
  /// </summary>
  private readonly SemaphoreSlim _writeLock = new(1, 1);

  /// <summary>
  ///   The oauth token to use to connect.
  /// </summary>
  private string _oauthToken;

  /// <summary>
  ///   The stream reader used to communicate with the IRC server.
  /// </summary>
  private StreamReader? _reader;

  /// <summary>
  ///   The cancellation source for the periodic rejoin loop.
  /// </summary>
  private CancellationTokenSource? _rejoinCancellation;

  /// <summary>
  ///   The task running the periodic rejoin loop.
  /// </summary>
  private Task? _rejoinTask;

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
    _rejoinCancellation?.Cancel();
    _rejoinCancellation?.Dispose();

    _writer?.Dispose();
    _reader?.Dispose();
    _sslStream?.Dispose();
    _tcpClient?.Dispose();

    _connectionLock.Dispose();
    _writeLock.Dispose();
  }

  /// <summary>
  ///   Connects to the twitch IRC server.
  /// </summary>
  public async Task ConnectAsync() {
    await _connectionLock.WaitAsync().ConfigureAwait(false);

    try {
      await DisconnectInternalAsync().ConfigureAwait(false);
      await ConnectInternalAsync().ConfigureAwait(false);

      StartRejoinLoop();
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

      StartRejoinLoop();
    }
    finally {
      _connectionLock.Release();
    }
  }

  /// <summary>
  ///   Sends a message to the twitch IRC channel.
  /// </summary>
  /// <param name="channel">The channel to send the message to.</param>
  /// <param name="message">The message to send.</param>
  /// <exception cref="InvalidOperationException">Not connected, call <see cref="ConnectAsync" /> with valid credentials.</exception>
  public async Task SendMessageAsync(string channel, string message) {
    await SendRawAsync($"PRIVMSG #{channel} :{message}").ConfigureAwait(false);
  }

  /// <summary>
  ///   Connects to a channel.
  /// </summary>
  /// <param name="channel">The channel to connect to.</param>
  public async Task AddChannelAsync(string channel) {
    if (_desiredChannels.TryAdd(channel, channel)) {
      try {
        await SendRawAsync($"JOIN #{channel}").ConfigureAwait(false);
      }
      catch {
        _desiredChannels.TryRemove(channel, out _);
        throw;
      }
    }
  }

  /// <summary>
  ///   Disconnects from a channel.
  /// </summary>
  /// <param name="channel">The channel to disconnect from.</param>
  public async Task RemoveChannelAsync(string channel) {
    if (_desiredChannels.TryRemove(channel, out _)) {
      _joinedChannels.TryRemove(channel, out _);

      try {
        await SendRawAsync($"PART #{channel}").ConfigureAwait(false);
      }
      catch (InvalidOperationException) {
        // The channel is no longer desired, so there is nothing left to do.
      }
    }
  }

  /// <summary>
  ///   Performs a blocking read loop to read messages from the IRC server.
  ///   Automatically reconnects if the connection is lost.
  /// </summary>
  /// <param name="onMessage">The function to call when a message is received.</param>
  /// <param name="onBan">The function to call when a ban is received.</param>
  /// <param name="token">The cancellation token to use for the read loop.</param>
  public async Task ReadLoopAsync(Func<string, Task>? onMessage = null, Func<string, Task>? onBan = null, CancellationToken token = default) {
    TimeSpan reconnectDelay = TimeSpan.FromSeconds(1);

    while (!token.IsCancellationRequested) {
      if (_reader == null) {
        try {
          await _connectionLock.WaitAsync(token).ConfigureAwait(false);
          try {
            if (_reader == null) {
              await ConnectInternalAsync().ConfigureAwait(false);
              StartRejoinLoop();
            }
          }
          finally {
            _connectionLock.Release();
          }

          reconnectDelay = TimeSpan.FromSeconds(1);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) {
          break;
        }
        catch (Exception ex) {
          LOG.Warn($"Failed to connect to twitch IRC. Retrying in {reconnectDelay.TotalSeconds:0} seconds.", ex);

          try {
            await Task.Delay(reconnectDelay, token).ConfigureAwait(false);
          }
          catch (OperationCanceledException) when (token.IsCancellationRequested) {
            break;
          }

          reconnectDelay = TimeSpan.FromSeconds(Math.Min(reconnectDelay.TotalSeconds * 2, 30));
          continue;
        }
      }

      StreamReader? reader = _reader;
      if (reader == null) {
        continue;
      }

      try {
        string? line = await reader.ReadLineAsync(token).ConfigureAwait(false);

        if (line == null) {
          LOG.Warn("The twitch IRC connection was closed unexpectedly.");
        }
        else {
          LOG.Debug($"<< {line}");

          if (line.StartsWith("PING ", StringComparison.InvariantCultureIgnoreCase)) {
            await SendRawAsync($"PONG {line["PING ".Length..]}").ConfigureAwait(false);
            continue;
          }

          if (line.Contains(":tmi.twitch.tv RECONNECT", StringComparison.InvariantCultureIgnoreCase)) {
            await _connectionLock.WaitAsync(token).ConfigureAwait(false);
            try {
              await DisconnectInternalAsync().ConfigureAwait(false);
              await ConnectInternalAsync().ConfigureAwait(false);

              StartRejoinLoop();
              reconnectDelay = TimeSpan.FromSeconds(1);
            }
            finally {
              _connectionLock.Release();
            }

            continue;
          }

          if (line.Contains(" JOIN #", StringComparison.InvariantCultureIgnoreCase) && IsOwnJoin(line, out string joinedChannel)) {
            _joinedChannels[joinedChannel] = 0;

            LOG.Debug($"Joined #{joinedChannel}");
          }
          else if (line.Contains(" PART #", StringComparison.InvariantCultureIgnoreCase) && IsOwnPart(line, out string partedChannel)) {
            _joinedChannels.TryRemove(partedChannel, out _);

            LOG.Debug($"Parted #{partedChannel}");
          }

          if (onBan != null && line.Contains(" CLEARCHAT ", StringComparison.InvariantCultureIgnoreCase) && line.Contains("target-user-id=", StringComparison.InvariantCultureIgnoreCase)) {
            await onBan(line).ConfigureAwait(false);
            continue;
          }

          if (onMessage != null) {
            await onMessage(line).ConfigureAwait(false);
          }

          continue;
        }
      }
      catch (OperationCanceledException) when (token.IsCancellationRequested) {
        break;
      }
      catch (IOException ex) {
        LOG.Warn("The twitch IRC connection was closed unexpectedly.", ex);
      }
      catch (ObjectDisposedException) {
        // The connection was closed while the read was in progress.
      }
      catch (InvalidOperationException ex) {
        LOG.Warn("The twitch IRC connection became unavailable.", ex);
      }

      if (token.IsCancellationRequested) {
        break;
      }

      await _connectionLock.WaitAsync(token).ConfigureAwait(false);
      try {
        await DisconnectInternalAsync().ConfigureAwait(false);
      }
      finally {
        _connectionLock.Release();
      }

      try {
        LOG.Warn($"Reconnecting to twitch IRC in {reconnectDelay.TotalSeconds:0} seconds.");

        await Task.Delay(reconnectDelay, token).ConfigureAwait(false);
      }
      catch (OperationCanceledException) when (token.IsCancellationRequested) {
        break;
      }

      reconnectDelay = TimeSpan.FromSeconds(Math.Min(reconnectDelay.TotalSeconds * 2, 30));
    }
  }

  /// <summary>
  ///   Connects to the twitch IRC server and joins any channels specified in <see cref="_desiredChannels" />.
  /// </summary>
  private async Task ConnectInternalAsync() {
    TcpClient tcpClient = new();
    SslStream? sslStream = null;
    StreamReader? reader = null;
    StreamWriter? writer = null;

    try {
      await tcpClient.ConnectAsync("irc.chat.twitch.tv", 6697).ConfigureAwait(false);

      sslStream = new SslStream(tcpClient.GetStream(), false);
      await sslStream.AuthenticateAsClientAsync("irc.chat.twitch.tv").ConfigureAwait(false);

      reader = new StreamReader(sslStream, new UTF8Encoding(false));
      writer = new StreamWriter(sslStream, new UTF8Encoding(false)) {
        AutoFlush = true,
        NewLine = "\r\n"
      };

      _tcpClient = tcpClient;
      _sslStream = sslStream;
      _reader = reader;
      _writer = writer;

      await SendRawAsync("CAP REQ :twitch.tv/tags twitch.tv/commands twitch.tv/membership").ConfigureAwait(false);

      await SendRawAsync($"PASS {_oauthToken}").ConfigureAwait(false);
      await SendRawAsync($"NICK {_username}").ConfigureAwait(false);

      _joinedChannels.Clear();

      foreach (string channel in _desiredChannels.Keys) {
        await SendRawAsync($"JOIN #{channel}").ConfigureAwait(false);
      }
    }
    catch {
      writer?.Dispose();
      reader?.Dispose();
      sslStream?.Dispose();
      tcpClient.Dispose();

      throw;
    }
  }

  /// <summary>
  ///   Disconnects from the twitch IRC server.
  /// </summary>
  private async Task DisconnectInternalAsync() {
    _rejoinCancellation?.Cancel();

    CancellationTokenSource? cancellation = _rejoinCancellation;
    Task? rejoinTask = _rejoinTask;

    _rejoinCancellation = null;
    _rejoinTask = null;

    if (rejoinTask != null) {
      try {
        await rejoinTask.ConfigureAwait(false);
      }
      catch (OperationCanceledException) when (cancellation?.IsCancellationRequested == true) {
        // Expected when the client is disconnected.
      }
    }

    cancellation?.Dispose();

    _joinedChannels.Clear();

    StreamWriter? writer = _writer;
    StreamReader? reader = _reader;
    SslStream? sslStream = _sslStream;
    TcpClient? tcpClient = _tcpClient;

    _writer = null;
    _reader = null;
    _sslStream = null;
    _tcpClient = null;

    try {
      if (writer != null) {
        await writer.FlushAsync().ConfigureAwait(false);
      }
    }
    catch {
      // The connection may already have been closed.
    }

    writer?.Dispose();
    reader?.Dispose();
    sslStream?.Dispose();
    tcpClient?.Dispose();
  }

  /// <summary>
  ///   Starts the periodic channel rejoin loop.
  /// </summary>
  private void StartRejoinLoop() {
    _rejoinCancellation?.Cancel();
    _rejoinCancellation?.Dispose();

    _rejoinCancellation = new CancellationTokenSource();
    _rejoinTask = RejoinLoopAsync(_rejoinCancellation.Token);
  }

  /// <summary>
  ///   Periodically checks that we're still joined to all requested channels.
  /// </summary>
  private async Task RejoinLoopAsync(CancellationToken token) {
    using PeriodicTimer timer = new(TimeSpan.FromMinutes(1));

    try {
      while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false)) {
        if (_writer == null) {
          continue;
        }

        foreach (string channel in _desiredChannels.Keys) {
          if (_joinedChannels.ContainsKey(channel)) {
            continue;
          }

          try {
            LOG.Warn($"Not currently joined to #{channel}; attempting to rejoin.");

            await SendRawAsync($"JOIN #{channel}").ConfigureAwait(false);
          }
          catch (InvalidOperationException) {
            // The connection was disconnected while attempting to rejoin.
            return;
          }
          catch (Exception ex) {
            LOG.Warn($"Failed to rejoin #{channel}.", ex);
          }
        }
      }
    }
    catch (OperationCanceledException) when (token.IsCancellationRequested) {
      // Expected when the client is disconnected or disposed.
    }
  }

  /// <summary>
  ///   Determines whether the given line is an event and returns the channel it occurred in.
  /// </summary>
  /// <param name="action">The action (e.g., PART or JOIN)</param>
  /// <param name="line">The IRC line to check.</param>
  /// <param name="channel">The channel the event occurred in.</param>
  /// <returns>True if is an event, false otherwise.</returns>
  private bool IsOwnEvent(string action, string line, out string channel) {
    channel = string.Empty;

    int spaceIndex = line.IndexOf(' ');
    if (spaceIndex <= 1) {
      return false;
    }

    string prefix = line[1..spaceIndex];
    int bangIndex = prefix.IndexOf('!');
    string nickname = bangIndex >= 0 ? prefix[..bangIndex] : prefix;

    if (!string.Equals(nickname, _username, StringComparison.InvariantCultureIgnoreCase)) {
      return false;
    }

    string joinMarker = $" {action} #";
    int joinIndex = line.IndexOf(joinMarker, StringComparison.InvariantCultureIgnoreCase);
    if (joinIndex < 0) {
      return false;
    }

    channel = line[(joinIndex + joinMarker.Length)..].Trim();

    int spaceAfterChannel = channel.IndexOf(' ');
    if (spaceAfterChannel >= 0) {
      channel = channel[..spaceAfterChannel];
    }

    return !string.IsNullOrWhiteSpace(channel);
  }

  /// <summary>
  ///   Determines whether an IRC message represents a JOIN by this client.
  /// </summary>
  /// <param name="line">The IRC message.</param>
  /// <param name="channel">The channel that was joined.</param>
  /// <returns><c>true</c> if this client joined the channel.</returns>
  private bool IsOwnJoin(string line, out string channel) {
    return IsOwnEvent("JOIN", line, out channel);
  }

  /// <summary>
  ///   Determines whether an IRC message represents a PART by this client.
  /// </summary>
  /// <param name="line">The IRC message.</param>
  /// <param name="channel">The channel that was parted.</param>
  /// <returns><c>true</c> if this client parted the channel.</returns>
  private bool IsOwnPart(string line, out string channel) {
    return IsOwnEvent("PART", line, out channel);
  }

  /// <summary>
  ///   Sends a raw IRC command to the server.
  /// </summary>
  /// <param name="command">The command.</param>
  /// <exception cref="InvalidOperationException">
  ///   Not connected, connect with <see cref="ConnectAsync" />.
  /// </exception>
  private async Task SendRawAsync(string command) {
    await _writeLock.WaitAsync().ConfigureAwait(false);

    try {
      StreamWriter? writer = _writer;
      if (writer == null) {
        throw new InvalidOperationException("Not connected.");
      }

      LOG.Debug($">> {command}");

      await writer.WriteLineAsync(command).ConfigureAwait(false);
    }
    finally {
      _writeLock.Release();
    }
  }

  /// <summary>
  ///   Normalizes the specification for oauth tokens.
  /// </summary>
  /// <param name="token">The token.</param>
  /// <returns>The normalized oauth token string.</returns>
  private static string NormalizeToken(string token) {
    return token.StartsWith("oauth:", StringComparison.InvariantCultureIgnoreCase) ? token : $"oauth:{token}";
  }
}