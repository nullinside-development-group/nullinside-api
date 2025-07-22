using System.Timers;

using log4net;

using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;

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
  private readonly Dictionary<string, Action<OnMessageReceivedArgs>?> _onMessageReceived = new();

  /// <summary>
  ///   The callback(s) to invoke when a channel is raided.
  /// </summary>
  private readonly Dictionary<string, Action<OnRaidNotificationArgs>?> _onRaid = new();

  /// <summary>
  ///   The callback(s) to invoke when a channel receives a ban message.
  /// </summary>
  private readonly Dictionary<string, Action<OnUserBannedArgs>?> _onUserBanReceived = new();

  /// <summary>
  ///   A timer used to re-connect the Twitch chat client.
  /// </summary>
  private readonly Timer _twitchChatClientReconnect;

  /// <summary>
  ///   The lock to prevent mutual exclusion on the <see cref="_client" /> object.
  /// </summary>
  private readonly object _twitchClientLock = new();

  /// <summary>
  ///   The twitch client to send and receive messages with.
  /// </summary>
  private TwitchClient? _client;

  /// <summary>
  ///   The callback(s) to invoke when the twitch chat client is disconnected from twitch chat.
  /// </summary>
  private Action? _onDisconnected;

  /// <summary>
  ///   The web socket to connect to twitch chat with.
  /// </summary>
  private WebSocketClient? _socket;

  /// <summary>
  ///   The twitch OAuth token to use to connect.
  /// </summary>
  private string? _twitchOAuthToken;

  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchClientProxy" /> class.
  /// </summary>
  protected TwitchClientProxy() {
    _joinedChannels = new HashSet<string>();

    // The timer for checking to make sure the IRC channel is connected.
    _twitchChatClientReconnect = new Timer(1000);
    _twitchChatClientReconnect.AutoReset = false;
    _twitchChatClientReconnect.Elapsed += TwitchChatClientReconnectOnElapsed;
    _twitchChatClientReconnect.Start();
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
  public string? TwitchUsername { get; set; }

  /// <inheritdoc />
  public string? TwitchOAuthToken {
    get => _twitchOAuthToken;
    set {
      if (value == _twitchOAuthToken) {
        return;
      }

      _twitchOAuthToken = value;

      // If we have a client, try to connect.
      if (null != _client) {
        _client.Disconnect();

        if (null != value) {
          _client.SetConnectionCredentials(new ConnectionCredentials(TwitchUsername, value));
          Connect();
        }
      }
    }
  }

  /// <inheritdoc />
  public void Dispose() {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  /// <inheritdoc />
  public async Task<bool> SendMessage(string channel, string message, uint retryConnection = 5) {
    // Sanity check.
    if (string.IsNullOrWhiteSpace(channel)) {
      return false;
    }

    // Try to connect and join the channel.
    bool connectedAndJoined = false;
    for (int i = 0; i < retryConnection; i++) {
      if (await JoinChannel(channel)) {
        connectedAndJoined = true;
        break;
      }
    }

    // If we failed to connect and join the channel, give up.
    if (!connectedAndJoined) {
      return false;
    }

    try {
      lock (_twitchClientLock) {
        // If we are not connected for some reason, we shouldn't have gotten here, so get out.
        if (null == _client ||
            !_client.IsConnected ||
            null == _client.JoinedChannels.FirstOrDefault(j =>
              channel.Equals(j.Channel, StringComparison.InvariantCultureIgnoreCase))) {
          return false;
        }

        LOG.Info($"{channel} Sending: {message}");
        _client.SendMessage(channel, message);
      }
    }
    catch {
      return false;
    }

    return true;
  }

  /// <inheritdoc />
  public async Task AddMessageCallback(string channel, Action<OnMessageReceivedArgs> callback) {
    await JoinChannel(channel);
    string channelSan = channel.ToLowerInvariant();
    lock (_onMessageReceived) {
      if (!_onMessageReceived.TryAdd(channelSan, callback)) {
        _onMessageReceived[channelSan] -= callback;
        _onMessageReceived[channelSan] += callback;
      }
    }
  }

  /// <inheritdoc />
  public void RemoveMessageCallback(string channel, Action<OnMessageReceivedArgs> callback) {
    bool shouldRemove = false;
    string channelSan = channel.ToLowerInvariant();
    lock (_onMessageReceived) {
      if (_onMessageReceived.ContainsKey(channelSan)) {
        Action<OnMessageReceivedArgs>? item = _onMessageReceived[channelSan];
        item -= callback;
        if (null == item) {
          _onMessageReceived.Remove(channelSan);
        }
        else {
          _onMessageReceived[channelSan] = item;
        }
      }
    }

    if (shouldRemove) {
      _client?.LeaveChannel(channelSan);

      // First add the channel to the master list.
      lock (_joinedChannels) {
        _joinedChannels.Add(channelSan);
      }
    }
  }

  /// <inheritdoc />
  public async Task AddBannedCallback(string channel, Action<OnUserBannedArgs> callback) {
    await JoinChannel(channel);

    lock (_onUserBanReceived) {
      _onUserBanReceived[channel] = callback;
    }
  }

  /// <inheritdoc />
  public void RemoveBannedCallback(string channel, Action<OnUserBannedArgs> callback) {
    lock (_onUserBanReceived) {
      _onUserBanReceived.Remove(channel);
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
  public async Task AddRaidCallback(string channel, Action<OnRaidNotificationArgs> callback) {
    await JoinChannel(channel);

    lock (_onRaid) {
      _onRaid[channel] = callback;
    }
  }

  /// <inheritdoc />
  public void RemoveRaidCallback(string channel, Action<OnRaidNotificationArgs> callback) {
    lock (_onRaid) {
      _onRaid.Remove(channel);
    }
  }

  /// <inheritdoc />
  public ValueTask DisposeAsync() {
    Dispose();
    return ValueTask.CompletedTask;
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
    if (!await Connect()) {
      return false;
    }

    try {
      // If we don't have a client, give up.
      if (null == _client) {
        return false;
      }

      lock (_twitchClientLock) {
        // If we are already in the channel, we are done.
        if (null != _client.JoinedChannels.FirstOrDefault(c =>
              channel.Equals(c.Channel, StringComparison.InvariantCultureIgnoreCase))) {
          return true;
        }

        // Otherwise, join the channel. At one point we waited here on the "OnJoinedChannel" to ensure the
        // connection before moving onto the next channel. However, it was causing a massive slowdown in
        // the application, and we've been working fine without it...so for now...we try this...
        _client.JoinChannel(channel);
      }

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
    await Connect();

    // Pull the master list of channels we should be connected to the stack.
    string[]? allChannels = null;
    lock (_joinedChannels) {
      allChannels = _joinedChannels.ToArray();
    }

    // Join all the channels.
    foreach (string channel in allChannels) {
      await JoinChannel(channel);
    }

    // Restart the timer.
    _twitchChatClientReconnect.Start();
  }

  /// <summary>
  ///   Connects to twitch chat.
  /// </summary>
  /// <returns>True if successful, false otherwise.</returns>
  private Task<bool> Connect() {
    // If we're already connected, we are good to go.
    lock (_twitchClientLock) {
      if (_client?.IsConnected ?? false) {
        return Task.FromResult(true);
      }
    }

    // If we don't have the ability to connect, we can leave early.
    if (string.IsNullOrWhiteSpace(TwitchUsername) || string.IsNullOrWhiteSpace(TwitchOAuthToken)) {
      return Task.FromResult(false);
    }

    try {
      bool isConnected = false;
      lock (_twitchClientLock) {
        if (_client?.IsConnected ?? false) {
          return Task.FromResult(true);
        }

        // If this is a first time initialization, create a brand-new client.
        bool haveNoClient = null == _client;
        if (haveNoClient) {
          var clientOptions = new ClientOptions
            { MessagesAllowedInPeriod = 750, ThrottlingPeriod = TimeSpan.FromSeconds(30) };

          _socket = new WebSocketClient(clientOptions);
          _client = new TwitchClient(_socket);
          var credentials = new ConnectionCredentials(TwitchUsername, TwitchOAuthToken);
          _client.Initialize(credentials);
          _client.AutoReListenOnException = true;
          _client.OnMessageReceived += TwitchChatClient_OnMessageReceived;
          _client.OnUserBanned += TwitchChatClient_OnUserBanned;
          _client.OnRaidNotification += TwitchChatClient_OnRaidNotification;
          _client.OnDisconnected += (sender, args) => {
            LOG.Error("Twitch Client Disconnected");
            _onDisconnected?.Invoke();
          };
          _client.OnConnectionError += (sender, args) => {
            LOG.Error($"Twitch Client Connection Error: {args.Error.Message}");
          };
          _client.OnError += (sender, args) => {
            LOG.Error("Twitch Client Error", args.Exception);
          };
          _client.OnIncorrectLogin += (sender, args) => {
            LOG.Error("Twitch Client Incorrect Login", args.Exception);
          };
          _client.OnNoPermissionError += (sender, args) => {
            LOG.Error("Twitch Client No Permission Error");
          };
        }

        try {
          // If we are not connect, connect.
          if (null != _client && !_client.IsConnected) {
            // If this is a new chat client, connect for the first time, otherwise reconnect.
            Action connect = haveNoClient ? () => _client.Connect() : () => _client.Reconnect();
            using var connectedEvent = new ManualResetEventSlim(false);
            EventHandler<OnConnectedArgs> onConnected = (_, _) => connectedEvent.Set();
            EventHandler<OnReconnectedEventArgs> onReconnect = (_, _) => connectedEvent.Set();
            try {
              _client!.OnConnected += onConnected;
              _client!.OnReconnected += onReconnect;
              connect();
              if (!connectedEvent.Wait(30 * 1000)) {
                return Task.FromResult(false);
              }
            }
            finally {
              _client.OnConnected -= onConnected;
              _client.OnReconnected -= onReconnect;
            }
          }
        }
        catch {
          // Do nothing, just try.
        }

        // Determine if we successfully connected.
        isConnected = _client?.IsConnected ?? false;
      }

      return Task.FromResult(isConnected);
    }
    catch {
      return Task.FromResult(false);
    }
  }

  /// <summary>
  ///   Handles when the channel receives a raid.
  /// </summary>
  /// <param name="sender">The twitch client.</param>
  /// <param name="e">The event arguments.</param>
  private void TwitchChatClient_OnRaidNotification(object? sender, OnRaidNotificationArgs e) {
    Action<OnRaidNotificationArgs>? callback;
    string channel = e.Channel.ToLowerInvariant();
    lock (_onRaid) {
      _onRaid.TryGetValue(channel, out callback);
    }

    Delegate[]? invokeList = callback?.GetInvocationList();
    if (null == invokeList) {
      return;
    }

    foreach (Delegate del in invokeList) {
      del.DynamicInvoke(e);
    }
  }

  /// <summary>
  ///   Handles when the channel receives a new chat message.
  /// </summary>
  /// <param name="sender">The twitch client.</param>
  /// <param name="e">The event arguments.</param>
  private void TwitchChatClient_OnMessageReceived(object? sender, OnMessageReceivedArgs e) {
    Action<OnMessageReceivedArgs>? callbacks = null;
    string channelSan = e.ChatMessage.Channel.ToLowerInvariant();
    lock (_onMessageReceived) {
      if (_onMessageReceived.TryGetValue(channelSan, out Action<OnMessageReceivedArgs>? messageReceivedCallback)) {
        callbacks = messageReceivedCallback;
      }
    }

    if (null == callbacks) {
      return;
    }

    Delegate[]? invokeList = callbacks?.GetInvocationList();
    if (null == invokeList) {
      return;
    }

    foreach (Delegate del in invokeList) {
      del.DynamicInvoke(e);
    }
  }

  /// <summary>
  ///   Handles when a channel receives a new ban.
  /// </summary>
  /// <param name="sender">The twitch client.</param>
  /// <param name="e">The event arguments.</param>
  private void TwitchChatClient_OnUserBanned(object? sender, OnUserBannedArgs e) {
    Action<OnUserBannedArgs>? callback;
    string channel = e.UserBan.Channel.ToLowerInvariant();
    lock (_onUserBanReceived) {
      _onUserBanReceived.TryGetValue(channel, out callback);
    }

    Delegate[]? invokeList = callback?.GetInvocationList();
    if (null == invokeList) {
      return;
    }

    foreach (Delegate del in invokeList) {
      del.DynamicInvoke(e);
    }
  }

  /// <summary>
  ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
  /// </summary>
  /// <param name="disposing">True if called directly, false if called from the destructor.</param>
  protected virtual void Dispose(bool disposing) {
    if (disposing) {
      _client?.Disconnect();
      _twitchChatClientReconnect?.Dispose();
      _socket?.Dispose();
    }

    s_instance = null;
  }
}