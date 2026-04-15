using System.Timers;

using log4net;

using Microsoft.Extensions.Logging;

using Nullinside.Api.Common.Twitch.Support;

using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

using OnConnectedEventArgs = TwitchLib.Communication.Events.OnConnectedEventArgs;
using Timer = System.Timers.Timer;

namespace Nullinside.Api.Common.Twitch;

using Timer = Timer;

/// <summary>
///   The singleton of a twitch chat messaging client.
/// </summary>
public class TwitchClientProxy : ITwitchClientProxy {
  /// <summary>
  ///   The logger.
  /// </summary>
  private static readonly ILog LOG = LogManager.GetLogger(typeof(TwitchClientProxy));

  /// <summary>
  ///   The singleton instance of the class.
  /// </summary>
  private static TwitchClientProxy? s_instance;

  /// <summary>
  ///   The twitch client to send and receive messages with.
  /// </summary>
  private TwitchClient _client;

  /// <summary>
  ///   The list of chats we attempted to join with the bot.
  /// </summary>
  /// <remarks>
  ///   We need to keep track of this separately so that we can make sure we re-join channels
  ///   in the <see cref="TwitchChatClientReconnectOnElapsed" /> method.
  /// </remarks>
  private readonly HashSet<string> _joinedChannels;

  /// <summary>
  ///   The callback(s) to invoke when a channel receives a chat message.
  /// </summary>
  private readonly Dictionary<string, Action<TwitchChatMessage>?> _onMessageReceived = new();

  /// <summary>
  ///   The callback(s) to invoke when a channel receives a ban message.
  /// </summary>
  private readonly Dictionary<string, Action<TwitchChatBan>?> _onUserBanReceived = new();

  /// <summary>
  ///   A timer used to re-connect the Twitch chat client.
  /// </summary>
  private readonly Timer _twitchChatClientReconnect;

  /// <summary>
  ///   The callback(s) to invoke when the twitch chat client is disconnected from twitch chat.
  /// </summary>
  private Action? _onDisconnected;

  /// <summary>
  ///   The twitch OAuth token to use to connect.
  /// </summary>
  private string? _twitchOAuthToken;

  /// <summary>
  ///   The twitch username that we are connected to twitch as.
  /// </summary>
  private string? _twitchUsername;

  private ILoggerFactory _loggerFactory;

  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchClientProxy" /> class.
  /// </summary>
  protected TwitchClientProxy() {
    _joinedChannels = new HashSet<string>();
    _loggerFactory = LoggerFactory.Create(c => c
        .AddConsole()
		//    .SetMinimumLevel(LogLevel.Trace) // uncomment to view raw messages received from twitch
    );
    
    _client = new TwitchClient();

    // The timer for checking to make sure the IRC channel is connected.
    _twitchChatClientReconnect = new Timer(1000);
    _twitchChatClientReconnect.AutoReset = false;
    _twitchChatClientReconnect.Elapsed += TwitchChatClientReconnectOnElapsed;
  }

  private async Task CreateTwitchClient() {
    if (_client.IsConnected) {
      await _client.DisconnectAsync().ConfigureAwait(false);
    }
    
    _client = new TwitchClient(loggerFactory: _loggerFactory);

    _client.OnMessageReceived += TwitchChatClient_OnMessageReceived;
    _client.OnUserBanned += TwitchChatClient_OnUserBanned;
    _client.OnConnected += (sender, args) => {
      LOG.Info("Twitch Client Connected");
      return Task.CompletedTask;
    };
    _client.OnDisconnected += (sender, args) => {
      LOG.Error("Twitch Client Disconnected");
      _onDisconnected?.Invoke();
      return Task.CompletedTask;
    };
    _client.OnConnectionError += (sender, args) => {
      LOG.Error($"Twitch Client Connection Error: {args.Error.Message}");
      return Task.CompletedTask;
    };
    _client.OnError += (sender, args) => {
      LOG.Error("Twitch Client Error", args.Exception);
      return Task.CompletedTask;
    };
    _client.OnIncorrectLogin += (sender, args) => {
      LOG.Error("Twitch Client Incorrect Login", args.Exception);
      return Task.CompletedTask;
    };
    _client.OnNoPermissionError += (sender, args) => {
      LOG.Error("Twitch Client No Permission Error");
      return Task.CompletedTask;
    };
  }

  /// <summary>
  ///   The singleton instance of the <see cref="TwitchClientProxy" /> class.
  /// </summary>
  public static TwitchClientProxy Instance {
    get {
      if (null == s_instance) {
        s_instance = new TwitchClientProxy();
      }

      return s_instance;
    }
  }

  /// <inheritdoc />
  public string? TwitchUsername {
    get => _twitchUsername;
    set {
      if (string.Equals(value, _twitchUsername)) {
        return;
      }

      _twitchUsername = value;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
      UpdateClientCredentialsAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
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

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
      UpdateClientCredentialsAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }
  }

  /// <inheritdoc />
  public void Dispose() {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  /// <inheritdoc />
  public async Task<bool> SendMessage(string channel, string message, uint retryConnection = 5) {
    if (!await TryJoinedChannel(channel, retryConnection).ConfigureAwait(false)) {
      return false;
    }

    try {
      // If we are not connected for some reason, we shouldn't have gotten here, so get out.
      if (!_client.IsConnected ||
          null == _client.JoinedChannels.FirstOrDefault(j =>
            channel.Equals(j.Channel, StringComparison.InvariantCultureIgnoreCase))) {
        return false;
      }

      LOG.Info($"{channel} Sending: {message}");
      await _client.SendMessageAsync(channel, message).ConfigureAwait(false);
    }
    catch {
      return false;
    }

    return true;
  }

  /// <inheritdoc />
  public async Task AddMessageCallback(string channel, Action<TwitchChatMessage> callback) {
    await JoinChannel(channel).ConfigureAwait(false);
    string channelSan = channel.ToLowerInvariant();
    lock (_onMessageReceived) {
      if (!_onMessageReceived.TryAdd(channelSan, callback)) {
        _onMessageReceived[channelSan] -= callback;
        _onMessageReceived[channelSan] += callback;
      }
    }
  }

  /// <inheritdoc />
  public void RemoveMessageCallback(string channel, Action<TwitchChatMessage> callback) {
    bool shouldRemove = false;
    string channelSan = channel.ToLowerInvariant();
    lock (_onMessageReceived) {
      if (_onMessageReceived.ContainsKey(channelSan)) {
        Action<TwitchChatMessage>? item = _onMessageReceived[channelSan];
        item -= callback;
        if (null == item) {
          _onMessageReceived.Remove(channelSan);
          shouldRemove = true;
        }
        else {
          _onMessageReceived[channelSan] = item;
        }
      }
    }

    if (shouldRemove) {
      _client.LeaveChannelAsync(channelSan);

      // Remove from the joined channels list
      lock (_joinedChannels) {
        _joinedChannels.Remove(channelSan);
      }
    }
  }

  /// <inheritdoc />
  public async Task AddBannedCallback(string channel, Action<TwitchChatBan> callback) {
    await JoinChannel(channel).ConfigureAwait(false);

    lock (_onUserBanReceived) {
      if (_onUserBanReceived.ContainsKey(channel)) {
        _onUserBanReceived[channel] += callback;
      }
      else {
        _onUserBanReceived[channel] = callback;
      }
    }
  }

  /// <inheritdoc />
  public void RemoveBannedCallback(string channel, Action<TwitchChatBan> callback) {
    lock (_onUserBanReceived) {
      if (!_onUserBanReceived.ContainsKey(channel)) {
        return;
      }

      _onUserBanReceived[channel] -= callback;

      if (null == _onUserBanReceived[channel]) {
        _onUserBanReceived.Remove(channel);
      }
    }
  }

  /// <inheritdoc />
  public void AddDisconnectedCallback(Action callback) {
    _onDisconnected -= callback;
    _onDisconnected += callback;
  }

  /// <inheritdoc />
  public void RemoveDisconnectedCallback(Action callback) {
    _onDisconnected -= callback;
  }

  /// <inheritdoc />
  public ValueTask DisposeAsync() {
    Dispose();
    return ValueTask.CompletedTask;
  }

  /// <summary>
  /// Updates the client credentials when getting a new oauth token or username.
  /// </summary>
  private async Task UpdateClientCredentialsAsync() {
    if (!_client.IsInitialized) {
      return;
    }

    try {
      await _client.DisconnectAsync().ContinueWith(async _ => {
        if (null == _twitchUsername || null == _twitchOAuthToken) {
          return;
        }

        _client.SetConnectionCredentials(new ConnectionCredentials(_twitchUsername, _twitchOAuthToken));
        await Connect().ConfigureAwait(false);
      }).ConfigureAwait(false);
    }
    catch (Exception) {
      // Do nothing, just try.
    }
  }

  /// <summary>
  ///   Determines if you've joined the channel already, attempts to connect if you have not.
  /// </summary>
  /// <param name="channel">The channel to join.</param>
  /// <param name="retryConnection">The number of times to retry.</param>
  /// <returns>True if joined successfully, false otherwise.</returns>
  private async Task<bool> TryJoinedChannel(string channel, uint retryConnection) {
    // Sanity check.
    if (string.IsNullOrWhiteSpace(channel)) {
      return false;
    }

    // Try to connect and join the channel.
    bool connectedAndJoined = false;
    for (int i = 0; i < retryConnection; i++) {
      if (await JoinChannel(channel).ConfigureAwait(false)) {
        connectedAndJoined = true;
        break;
      }
    }

    // If we failed to connect and join the channel, give up.
    if (!connectedAndJoined) {
      return false;
    }

    return true;
  }

  /// <summary>
  ///   Joins a twitch channel.
  /// </summary>
  /// <param name="channel">The channel to join.</param>
  /// <returns>True if connected and joined, false otherwise.</returns>
  private async Task<bool> JoinChannel(string channel) {
    // First add the channel to the master list.
    lock (_joinedChannels) {
      _joinedChannels.Add(channel.ToLowerInvariant());
    }

    // Try to connect.
    if (!await Connect().ConfigureAwait(false)) {
      return false;
    }

    try {
      // If we are already in the channel, we are done.
      if (null != _client.JoinedChannels.FirstOrDefault(c =>
            channel.Equals(c.Channel, StringComparison.InvariantCultureIgnoreCase))) {
        return true;
      }

      // Otherwise, join the channel. At one point we waited here on the "OnJoinedChannel" to ensure the
      // connection before moving onto the next channel. However, it was causing a massive slowdown in
      // the application, and we've been working fine without it...so for now...we try this...
      await _client.JoinChannelAsync(channel).ConfigureAwait(false);
      return true;
    }
    catch {
      return false;
    }
  }

  /// <summary>
  ///   Handles periodically checking if we are connected to twitch chats and reconnects to them.
  /// </summary>
  /// <param name="sender">The timer.</param>
  /// <param name="e">The event arguments.</param>
  private async void TwitchChatClientReconnectOnElapsed(object? sender, ElapsedEventArgs e) {
    // Connect the chat client.
    if (!await Connect().ConfigureAwait(false)) {
      return;
    }

    // Pull the master list of channels we should be connected to the stack.
    string[]? allChannels = null;
    lock (_joinedChannels) {
      allChannels = _joinedChannels.ToArray();
    }

    // Join all the channels.
    foreach (string channel in allChannels) {
      await JoinChannel(channel).ConfigureAwait(false);
    }

    // Restart the timer.
    _twitchChatClientReconnect.Start();
  }

  /// <summary>
  ///   Connects to twitch chat.
  /// </summary>
  /// <returns>True if successful, false otherwise.</returns>
  private async Task<bool> Connect() {
    // If we're already connected, we are good to go.
    if (_client.IsConnected) {
      return true;
    }

    // If we don't have the ability to connect, we can leave early.
    if (string.IsNullOrWhiteSpace(TwitchUsername) || string.IsNullOrWhiteSpace(TwitchOAuthToken)) {
      return false;
    }

    try {
      await CreateTwitchClient().ConfigureAwait(false);
      var credentials = new ConnectionCredentials(TwitchUsername, TwitchOAuthToken);
      if (!_client.IsInitialized) {
        _client.Initialize(credentials);
      }
      
      try {
        using var connectedEvent = new ManualResetEventSlim(false);
        Task OnConnected(object? sender, TwitchLib.Client.Events.OnConnectedEventArgs e) {
          connectedEvent.Set();
          return Task.CompletedTask;
        }
        
        try {
          _client!.OnConnected += OnConnected;
          await _client.ConnectAsync().ConfigureAwait(false);
          if (!connectedEvent.Wait(30 * 1000)) {
            return false;
          }
        }
        finally {
          _client.OnConnected -= OnConnected;
        }
      }
      catch {
        // Do nothing, just try.
      }
      
      _twitchChatClientReconnect.Stop();
      _twitchChatClientReconnect.Start();
      return _client.IsConnected;
    }
    catch {
      return false;
    }
  }

  /// <summary>
  ///   Handles when the channel receives a new chat message.
  /// </summary>
  /// <param name="sender">The twitch client.</param>
  /// <param name="e">The event arguments.</param>
  private Task TwitchChatClient_OnMessageReceived(object? sender, OnMessageReceivedArgs e) {
    Action<TwitchChatMessage>? callbacks = null;
    string channelSan = e.ChatMessage.Channel.ToLowerInvariant();
    lock (_onMessageReceived) {
      if (_onMessageReceived.TryGetValue(channelSan, out Action<TwitchChatMessage>? messageReceivedCallback)) {
        callbacks = messageReceivedCallback;
      }
    }

    if (null == callbacks) {
      return Task.CompletedTask;
    }

    Delegate[]? invokeList = callbacks?.GetInvocationList();
    if (null == invokeList) {
      return Task.CompletedTask;
    }

    foreach (Delegate del in invokeList) {
      del.DynamicInvoke(new TwitchChatMessage(e.ChatMessage));
    }

    return Task.CompletedTask;
  }

  /// <summary>
  ///   Handles when a channel receives a new ban.
  /// </summary>
  /// <param name="sender">The twitch client.</param>
  /// <param name="e">The event arguments.</param>
  private Task TwitchChatClient_OnUserBanned(object? sender, OnUserBannedArgs e) {
    Action<TwitchChatBan>? callback;
    string channel = e.UserBan.Channel.ToLowerInvariant();
    lock (_onUserBanReceived) {
      _onUserBanReceived.TryGetValue(channel, out callback);
    }

    Delegate[]? invokeList = callback?.GetInvocationList();
    if (null == invokeList) {
      return Task.CompletedTask;
    }

    foreach (Delegate del in invokeList) {
      del.DynamicInvoke(e);
    }

    return Task.CompletedTask;
  }

  /// <summary>
  ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
  /// </summary>
  /// <param name="disposing">True if called directly, false if called from the destructor.</param>
  protected virtual void Dispose(bool disposing) {
    if (disposing) {
      if (_client.IsConnected) {
        Task.WaitAll(_client.DisconnectAsync());
      }

      _twitchChatClientReconnect?.Dispose();
    }

    s_instance = null;
  }
}