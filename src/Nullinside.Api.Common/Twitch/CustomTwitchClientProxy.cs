using System.Collections.Concurrent;

using log4net;

using Nullinside.Api.Common.Twitch.Support;

namespace Nullinside.Api.Common.Twitch;

/// <summary>
///   A custom twitch client implementation.
/// </summary>
public class CustomTwitchClientProxy : ITwitchClientProxy {
  /// <summary>
  ///   The logger.
  /// </summary>
  private static readonly ILog LOG = LogManager.GetLogger(typeof(CustomTwitchClientProxy));

  /// <summary>
  ///   The callbacks when a ban is received.
  /// </summary>
  private readonly ConcurrentDictionary<string, Action<TwitchChatBan>> _banCallbacks = new();

  /// <summary>
  ///   The callbacks when a message is received.
  /// </summary>
  private readonly ConcurrentDictionary<string, Action<TwitchChatMessage>> _messageCallbacks = new();

  /// <summary>
  ///   True if the client has been disposed.
  /// </summary>
  private readonly CancellationTokenSource disposed = new();

  /// <summary>
  ///   The twitch irc client.
  /// </summary>
  private TwitchIrcClient? _client;

  /// <summary>
  ///   The callbacks when the client disconnects.
  /// </summary>
  private Action? _disconnectCallback;

  /// <summary>
  ///   The task that reads the irc client.
  /// </summary>
  private Task _ircClientReader;

  /// <summary>
  ///   The twitch oauth token.
  /// </summary>
  private string? _twitchOAuthToken;

  /// <summary>
  ///   The twitch username.
  /// </summary>
  private string? _twitchUsername;

  /// <summary>
  ///   Initializes a new instance of the <see cref="CustomTwitchClientProxy" /> class.
  /// </summary>
  public CustomTwitchClientProxy() {
    // Run the reader thread to read the irc client whenever it's available.
    _ircClientReader = Task.Run(async () => {
      while (!disposed.IsCancellationRequested) {
        TwitchIrcClient? client = _client;
        if (null != client) {
          try {
            await client.ReadLoopAsync(OnRawIrcReceived, OnBanReceived, disposed.Token).ConfigureAwait(false);
          }
          catch (Exception ex) {
            LOG.Error("Error ReadLoopAsync on irc reader", ex);
          }
        }
        else {
          await Task.Delay(250).ConfigureAwait(false);
        }
      }
    });
  }


  /// <inheritdoc />
  public string? TwitchUsername {
    get => _twitchUsername;
    set {
      if (value == _twitchUsername) {
        return;
      }

      _twitchUsername = value;
      if (TwitchUsername != null && TwitchOAuthToken != null) {
        if (null == _client) {
          _client = new TwitchIrcClient(TwitchUsername, TwitchOAuthToken);
          _client.ConnectAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
        else {
          _client.UpdateCredentialsAsync(TwitchUsername, TwitchOAuthToken).ConfigureAwait(false).GetAwaiter().GetResult();
        }
      }
    }
  }

  /// <inheritdoc />
  public string? TwitchOAuthToken {
    get => _twitchOAuthToken;
    set {
      if (value == _twitchOAuthToken) {
        return;
      }

      _twitchOAuthToken = value;
      if (TwitchUsername != null && TwitchOAuthToken != null) {
        if (null == _client) {
          _client = new TwitchIrcClient(TwitchUsername, TwitchOAuthToken);
          _client.ConnectAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
        else {
          _client.UpdateCredentialsAsync(TwitchUsername, TwitchOAuthToken).ConfigureAwait(false).GetAwaiter().GetResult();
        }
      }
    }
  }

  /// <inheritdoc />
  public void Dispose() {
    disposed.Cancel();
    _client?.Dispose();
    _client = null;
  }

  /// <inheritdoc />
  public ValueTask DisposeAsync() {
    disposed.Cancel();
    _client?.Dispose();
    _client = null;
    return default;
  }

  /// <inheritdoc />
  public async Task<bool> SendMessage(string channel, string message, uint retryConnection = 5) {
    TwitchIrcClient? client = _client;
    if (null == client) {
      return false;
    }

    await client.SendMessageAsync(channel, message).ConfigureAwait(false);
    return true;
  }

  /// <inheritdoc />
  public async Task AddMessageCallback(string channel, Action<TwitchChatMessage> callback) {
    channel = channel.ToLowerInvariant();
    _messageCallbacks.AddOrUpdate(channel, callback, (_, existing) => {
      if (existing.GetInvocationList().Contains(callback)) {
        return existing;
      }

      return existing + callback;
    });

    TwitchIrcClient? client = _client;
    if (null == client) {
      return;
    }

    await client.AddChannelAsync(channel).ConfigureAwait(false);
  }

  /// <inheritdoc />
  public async Task AddBannedCallback(string channel, Action<TwitchChatBan> callback) {
    channel = channel.ToLowerInvariant();
    _banCallbacks.AddOrUpdate(channel, callback, (_, existing) => {
      if (existing.GetInvocationList().Contains(callback)) {
        return existing;
      }

      return existing + callback;
    });
  }

  /// <inheritdoc />
  public async Task RemoveBannedCallback(string channel, Action<TwitchChatBan> callback) {
    channel = channel.ToLowerInvariant();
    if (!_banCallbacks.TryGetValue(channel, out Action<TwitchChatBan>? existing)) {
      return;
    }

    existing -= callback;
    if (existing == null) {
      _banCallbacks.TryRemove(channel, out _);
    }
    else {
      _banCallbacks[channel] = existing;
    }
  }

  /// <inheritdoc />
  public void AddDisconnectedCallback(Action callback) {
    _disconnectCallback += callback;
  }

  /// <inheritdoc />
  public void RemoveDisconnectedCallback(Action callback) {
    _disconnectCallback -= callback;
  }

  /// <inheritdoc />
  public async Task Connect() {
    if (_client == null) {
      return;
    }

    await _client.ConnectAsync().ConfigureAwait(false);
  }

  /// <inheritdoc />
  public async Task Disconnect() {
    if (_client == null) {
      return;
    }

    await _client.DisconnectAsync().ConfigureAwait(false);
  }

  /// <inheritdoc />
  public async Task RemoveMessageCallback(string channel, Action<TwitchChatMessage> callback) {
    channel = channel.ToLowerInvariant();
    if (!_messageCallbacks.TryGetValue(channel, out Action<TwitchChatMessage>? existing)) {
      return;
    }

    existing -= callback;
    if (existing == null) {
      _messageCallbacks.TryRemove(channel, out _);

      TwitchIrcClient? client = _client;
      if (null == client) {
        return;
      }

      await client.RemoveChannelAsync(channel).ConfigureAwait(false);
    }
    else {
      _messageCallbacks[channel] = existing;
    }
  }

  /// <summary>
  ///   Called whenever a raw irc message is received.
  /// </summary>
  /// <param name="irc">The irc message.</param>
  /// <returns>A completed task.</returns>
  private Task OnRawIrcReceived(string irc) {
    var message = new TwitchChatMessage(irc);
    if (!_messageCallbacks.TryGetValue(message.Channel, out Action<TwitchChatMessage>? callbacks) || callbacks.GetInvocationList().Length < 1) {
      return Task.CompletedTask;
    }

    foreach (Delegate callback in callbacks.GetInvocationList()) {
      try {
        callback.DynamicInvoke(message);
      }
      catch (Exception ex) {
        LOG.Error($"Error in message callback for {message.Channel}: {message.Message}", ex);
      }
    }

    return Task.CompletedTask;
  }

  /// <summary>
  ///   Called whenever a ban is received.
  /// </summary>
  /// <param name="irc">The irc message.</param>
  /// <returns></returns>
  /// <exception cref="NotImplementedException"></exception>
  private Task OnBanReceived(string irc) {
    var ban = new TwitchChatBan(irc);
    if (!_banCallbacks.TryGetValue(ban.Channel, out Action<TwitchChatBan>? callbacks) || callbacks.GetInvocationList().Length < 1) {
      return Task.CompletedTask;
    }

    foreach (Delegate callback in callbacks.GetInvocationList()) {
      try {
        callback.DynamicInvoke(ban);
      }
      catch (Exception ex) {
        LOG.Error($"Error in ban callback for {ban.Channel}: {ban.Username}", ex);
      }
    }

    return Task.CompletedTask;
  }
}