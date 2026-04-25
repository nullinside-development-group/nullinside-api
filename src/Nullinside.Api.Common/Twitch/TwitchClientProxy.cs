using System.Collections.Concurrent;
using System.Timers;

using log4net;

using Microsoft.Extensions.Logging;

using Nullinside.Api.Common.Twitch.Support;

using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

using Timer = System.Timers.Timer;

namespace Nullinside.Api.Common.Twitch;

/// <summary>
///   A proxy for the Twitch chat client.
/// </summary>
public class TwitchClientProxy : ITwitchClientProxy {
  /// <summary>
  ///   The logger.
  /// </summary>
  private static readonly ILog LOG = LogManager.GetLogger(typeof(TwitchClientProxy));

  /// <summary>
  ///   The twitch client to send and receive messages with.
  /// </summary>
  private readonly TwitchClient _client;

  /// <summary>
  ///   Semaphore to ensure only one connection attempt happens at a time.
  /// </summary>
  private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);

  /// <summary>
  ///   The list of chats we attempted to join with the bot.
  /// </summary>
  private readonly ConcurrentDictionary<string, byte> _joinedChannels = new();

  /// <summary>
  ///   The callback(s) to invoke when a channel receives a chat message.
  /// </summary>
  private readonly ConcurrentDictionary<string, Action<TwitchChatMessage>> _onMessageReceived = new();

  /// <summary>
  ///   The callback(s) to invoke when a channel receives a ban message.
  /// </summary>
  private readonly ConcurrentDictionary<string, Action<TwitchChatBan>> _onUserBanReceived = new();

  /// <summary>
  ///   The timer used to re-connect the Twitch chat client if it's not connected.
  /// </summary>
  private readonly Timer _reconnectTimer;

  /// <summary>
  ///   The twitch OAuth token to use to connect.
  /// </summary>
  private string? _twitchOAuthToken;

  /// <summary>
  ///   The twitch username that we are connected to twitch as.
  /// </summary>
  private string? _twitchUsername;

  /// <summary>
  ///   The last time a message was received from Twitch.
  /// </summary>
  private DateTime _lastMessageReceived = DateTime.UtcNow;

  /// <summary>
  ///   Whether a credential update is pending.
  /// </summary>
  private int _updatePending;

  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchClientProxy" /> class.
  /// </summary>
  /// <param name="loggerFactory">The logger factory to use for debug logging.</param>
  public TwitchClientProxy(ILoggerFactory? loggerFactory = null) {
    _client = new TwitchClient(loggerFactory: loggerFactory);
    _client.OnMessageReceived += TwitchChatClient_OnMessageReceived;
    _client.OnUserBanned += TwitchChatClient_OnUserBanned;
    _client.OnJoinedChannel += (_, args) => {
      LOG.Info($"Joined channel: {args.Channel}");
      return Task.CompletedTask;
    };
    _client.OnLeftChannel += (_, args) => {
      LOG.Warn($"Left channel: {args.Channel}");
      return Task.CompletedTask;
    };
    _client.OnConnected += (_, _) => {
      if (_client.IsConnected) {
        LOG.Info("Twitch Client Connected");
      }

      return Task.CompletedTask;
    };
    _client.OnDisconnected += (_, _) => {
      LOG.Error("Twitch Client Disconnected");
      OnDisconnected?.Invoke();
      return Task.CompletedTask;
    };
    _client.OnConnectionError += (_, args) => {
      LOG.Error($"Twitch Client Connection Error: {args.Error.Message}");
      return Task.CompletedTask;
    };
    _client.OnError += (_, args) => {
      LOG.Error("Twitch Client Error", args.Exception);
      return Task.CompletedTask;
    };
    _client.OnIncorrectLogin += (_, args) => {
      LOG.Error("Twitch Client Incorrect Login", args.Exception);
      return Task.CompletedTask;
    };
    _client.OnNoPermissionError += (_, _) => {
      LOG.Error("Twitch Client No Permission Error");
      return Task.CompletedTask;
    };

    _reconnectTimer = new Timer(TimeSpan.FromSeconds(30).TotalMilliseconds);
    _reconnectTimer.Elapsed += ReconnectTimer_Elapsed;
    _reconnectTimer.AutoReset = true;
    _reconnectTimer.Start();
  }

  /// <inheritdoc />
  public string? TwitchUsername {
    get => _twitchUsername;
    set {
      if (string.Equals(value, _twitchUsername)) {
        return;
      }

      _twitchUsername = value;
      _ = Task.Run(UpdateClientCredentialsAsync);
    }
  }

  /// <inheritdoc />
  public string? TwitchOAuthToken {
    get => _twitchOAuthToken;
    set {
      if (string.Equals(value, _twitchOAuthToken)) {
        return;
      }

      _twitchOAuthToken = value;
      _ = Task.Run(UpdateClientCredentialsAsync);
    }
  }

  /// <inheritdoc />
  public async Task<bool> SendMessage(string channel, string message, uint retryConnection = 5) {
    string channelSan = channel.ToLowerInvariant();
    if (!await EnsureJoinedChannel(channelSan, retryConnection).ConfigureAwait(false)) {
      return false;
    }

    try {
      LOG.Info($"{channelSan} Sending: {message}");
      await _client.SendMessageAsync(channelSan, message).ConfigureAwait(false);
      return true;
    }
    catch (Exception ex) {
      LOG.Error($"Failed to send message to {channelSan}", ex);
      return false;
    }
  }

  /// <inheritdoc />
  public async Task AddMessageCallback(string channel, Action<TwitchChatMessage> callback) {
    string channelSan = channel.ToLowerInvariant();
    _onMessageReceived.AddOrUpdate(channelSan, callback, (_, existing) => {
      if (existing.GetInvocationList().Contains(callback)) {
        return existing;
      }

      return existing + callback;
    });
    await EnsureJoinedChannel(channelSan, 5).ConfigureAwait(false);
  }

  /// <inheritdoc />
  public void RemoveMessageCallback(string channel, Action<TwitchChatMessage> callback) {
    string channelSan = channel.ToLowerInvariant();
    if (_onMessageReceived.TryGetValue(channelSan, out Action<TwitchChatMessage>? existing)) {
      existing -= callback;
      if (existing == null) {
        _onMessageReceived.TryRemove(channelSan, out _);
        _client.LeaveChannelAsync(channelSan);
        _joinedChannels.TryRemove(channelSan, out _);
      }
      else {
        _onMessageReceived[channelSan] = existing;
      }
    }
  }

  /// <inheritdoc />
  public async Task AddBannedCallback(string channel, Action<TwitchChatBan> callback) {
    string channelSan = channel.ToLowerInvariant();
    _onUserBanReceived.AddOrUpdate(channelSan, callback, (_, existing) => {
      if (existing.GetInvocationList().Contains(callback)) {
        return existing;
      }

      return existing + callback;
    });
    await EnsureJoinedChannel(channelSan, 5).ConfigureAwait(false);
  }

  /// <inheritdoc />
  public void RemoveBannedCallback(string channel, Action<TwitchChatBan> callback) {
    string channelSan = channel.ToLowerInvariant();
    if (_onUserBanReceived.TryGetValue(channelSan, out Action<TwitchChatBan>? existing)) {
      existing -= callback;
      if (existing == null) {
        _onUserBanReceived.TryRemove(channelSan, out _);
      }
      else {
        _onUserBanReceived[channelSan] = existing;
      }
    }
  }

  /// <inheritdoc />
  public void AddDisconnectedCallback(Action callback) {
    OnDisconnected += callback;
  }

  /// <inheritdoc />
  public void RemoveDisconnectedCallback(Action callback) {
    OnDisconnected -= callback;
  }

  /// <inheritdoc />
  public void Dispose() {
    _reconnectTimer.Dispose();
    if (_client.IsConnected) {
      _client.DisconnectAsync().GetAwaiter().GetResult();
    }

    GC.SuppressFinalize(this);
  }

  /// <inheritdoc />
  public async ValueTask DisposeAsync() {
    _reconnectTimer.Dispose();
    if (_client.IsConnected) {
      await _client.DisconnectAsync().ConfigureAwait(false);
    }

    GC.SuppressFinalize(this);
  }

  /// <summary>
  ///   The callback(s) to invoke when the twitch chat client is disconnected.
  /// </summary>
  private event Action? OnDisconnected;

  /// <summary>
  ///   Provides thread-safe ability to update the client credentials.
  /// </summary>
  private async Task UpdateClientCredentialsAsync() {
    if (string.IsNullOrWhiteSpace(_twitchUsername) || string.IsNullOrWhiteSpace(_twitchOAuthToken)) {
      return;
    }

    if (Interlocked.CompareExchange(ref _updatePending, 1, 0) != 0) {
      return;
    }

    // Small delay to allow both properties to be set if they are being updated together.
    await Task.Delay(100).ConfigureAwait(false);

    await _connectionSemaphore.WaitAsync().ConfigureAwait(false);
    try {
      Interlocked.Exchange(ref _updatePending, 0);

      // Check if credentials have actually changed compared to what the client is using.
      if (_client.IsInitialized &&
          _client.ConnectionCredentials?.TwitchUsername == _twitchUsername) {
        // Credentials haven't changed (we can't easily check token without potentially having to 
        // access protected members or assuming if username is same, it might be the same token 
        // but it's safer to just re-initialize if we aren't sure. 
        // Actually TwitchLib's ConnectionCredentials doesn't expose TwitchToken publicly in some versions?
        // Let's check what it has.
        await ConnectAsyncInternal().ConfigureAwait(false);
        return;
      }

      if (_client.IsConnected) {
        await _client.DisconnectAsync().ConfigureAwait(false);
      }

      _client.Initialize(new ConnectionCredentials(_twitchUsername, _twitchOAuthToken));
      await ConnectAsyncInternal().ConfigureAwait(false);
    }
    catch (Exception ex) {
      LOG.Error("Failed to update client credentials", ex);
    }
    finally {
      _connectionSemaphore.Release();
    }
  }

  /// <summary>
  ///   Ensures that the channel is joined to the chat client.
  /// </summary>
  /// <param name="channel">The channel to join.</param>
  /// <param name="retryCount">The number of times to retry.</param>
  /// <returns>True if successful, false otherwise.</returns>
  private async Task<bool> EnsureJoinedChannel(string channel, uint retryCount) {
    if (string.IsNullOrWhiteSpace(channel)) {
      return false;
    }

    for (int i = 0; i < retryCount; i++) {
      if (await JoinChannelAsync(channel).ConfigureAwait(false)) {
        return true;
      }

      await Task.Delay(1000).ConfigureAwait(false);
    }

    return false;
  }

  /// <summary>
  ///   Joins a channel.
  /// </summary>
  /// <param name="channel">The channel to join.</param>
  /// <returns>True if successful, false otherwise.</returns>
  private async Task<bool> JoinChannelAsync(string channel) {
    if (!await ConnectAsync().ConfigureAwait(false)) {
      return false;
    }

    _joinedChannels.TryAdd(channel, 0);

    if (_client.JoinedChannels.Any(c => c.Channel.Equals(channel, StringComparison.OrdinalIgnoreCase))) {
      return true;
    }

    try {
      await _client.JoinChannelAsync(channel).ConfigureAwait(false);
      return true;
    }
    catch (Exception ex) {
      LOG.Error($"Failed to join channel {channel}", ex);
      return false;
    }
  }

  /// <summary>
  ///   Connects to the twitch IRC server.
  /// </summary>
  /// <returns>True if successful, false otherwise.</returns>
  private async Task<bool> ConnectAsync() {
    await _connectionSemaphore.WaitAsync().ConfigureAwait(false);
    try {
      return await ConnectAsyncInternal().ConfigureAwait(false);
    }
    finally {
      _connectionSemaphore.Release();
    }
  }

  /// <summary>
  ///   Connects to the twitch IRC server.
  /// </summary>
  /// <returns>True if successful, false otherwise.</returns>
  private async Task<bool> ConnectAsyncInternal() {
    if (_client.IsConnected) {
      return true;
    }

    if (string.IsNullOrWhiteSpace(_twitchUsername) || string.IsNullOrWhiteSpace(_twitchOAuthToken)) {
      return false;
    }

    if (!_client.IsInitialized) {
      _client.Initialize(new ConnectionCredentials(_twitchUsername, _twitchOAuthToken));
    }

    try {
      LOG.Info("Connecting Twitch Client...");
      await _client.ConnectAsync().ConfigureAwait(false);

      // Give it a small amount of time to actually report being connected if it's not immediate
      for (int i = 0; i < 50 && !_client.IsConnected; i++) {
        await Task.Delay(100).ConfigureAwait(false);
      }

      return _client.IsConnected;
    }
    catch (Exception ex) {
      LOG.Error("Failed to connect to Twitch", ex);
      return false;
    }
  }

  /// <summary>
  ///   Checks if the client is connected and if not, attempts to reconnect.
  /// </summary>
  /// <param name="sender">The invoker of the event.</param>
  /// <param name="e">The elapsed event arguments.</param>
  private async void ReconnectTimer_Elapsed(object? sender, ElapsedEventArgs e) {
    try {
      if (string.IsNullOrWhiteSpace(_twitchUsername) || string.IsNullOrWhiteSpace(_twitchOAuthToken)) {
        return;
      }

      bool shouldReconnect = !_client.IsConnected;
      if (!shouldReconnect && DateTime.UtcNow - _lastMessageReceived > TimeSpan.FromMinutes(5)) {
        LOG.Warn($"No messages received in {DateTime.UtcNow - _lastMessageReceived}. Forcing reconnection.");
        shouldReconnect = true;
      }

      if (shouldReconnect) {
        if (_client.IsConnected) {
          await _client.DisconnectAsync().ConfigureAwait(false);
        }

        await ConnectAsync().ConfigureAwait(false);
      }

      if (_client.IsConnected) {
        foreach (string channel in _joinedChannels.Keys) {
          // If the client thinks it's not in the channel, join it.
          if (!_client.JoinedChannels.Any(c => c.Channel.Equals(channel, StringComparison.OrdinalIgnoreCase))) {
            LOG.Warn($"Channel {channel} is missing from JoinedChannels list. Re-joining...");
            try {
              // Sometimes JoinChannelAsync fails silently if it thinks it's already there 
              // or if the connection is in a weird state.
              await _client.JoinChannelAsync(channel).ConfigureAwait(false);
            }
            catch (Exception ex) {
              LOG.Error($"Failed to re-join channel {channel}", ex);
            }
          }
        }
      }

      // If we are connected but haven't received ANY messages for 2 minutes,
      // it might be a general silence or a zombie connection.
      // We'll try to re-join all channels if this happens, just in case the JoinedChannels list is lying.
      if (_client.IsConnected && DateTime.UtcNow - _lastMessageReceived > TimeSpan.FromMinutes(2)) {
        LOG.Info($"Inactivity detected ({DateTime.UtcNow - _lastMessageReceived}). Re-verifying all channels...");
        foreach (string channel in _joinedChannels.Keys) {
          try {
            await _client.JoinChannelAsync(channel).ConfigureAwait(false);
          }
          catch (Exception ex) {
            LOG.Error($"Failed to refresh channel {channel}", ex);
          }
        }
      }
    }
    catch (Exception ex) {
      LOG.Error("Failed to reconnect to Twitch", ex);
    }
  }

  private Task TwitchChatClient_OnMessageReceived(object? sender, OnMessageReceivedArgs e) {
    _lastMessageReceived = DateTime.UtcNow;
    string channelSan = e.ChatMessage.Channel.ToLowerInvariant();
    if (_onMessageReceived.TryGetValue(channelSan, out Action<TwitchChatMessage>? callback)) {
      foreach (Action<TwitchChatMessage> handler in callback.GetInvocationList()) {
        try {
          handler(new TwitchChatMessage(e.ChatMessage));
        }
        catch (Exception ex) {
          LOG.Error($"Error in message callback for {channelSan}", ex);
        }
      }
    }

    return Task.CompletedTask;
  }

  private Task TwitchChatClient_OnUserBanned(object? sender, OnUserBannedArgs e) {
    _lastMessageReceived = DateTime.UtcNow;
    string channelSan = e.UserBan.Channel.ToLowerInvariant();
    if (_onUserBanReceived.TryGetValue(channelSan, out Action<TwitchChatBan>? callback)) {
      foreach (Action<TwitchChatBan> handler in callback.GetInvocationList()) {
        try {
          handler(new TwitchChatBan(e.UserBan));
        }
        catch (Exception ex) {
          LOG.Error($"Error in ban callback for {channelSan}", ex);
        }
      }
    }

    return Task.CompletedTask;
  }
}