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
  private static TwitchClientProxy? instance;

  /// <summary>
  ///   The list of chats we attempted to join with the bot.
  /// </summary>
  /// <remarks>
  ///   We need to keep track of this separately so that we can make sure we re-join channels
  ///   in the <see cref="TwitchChatClientReconnectOnElapsed" /> method.
  /// </remarks>
  private readonly HashSet<string> joinedChannels;

  /// <summary>
  ///   A timer used to re-connect the Twitch chat client.
  /// </summary>
  private readonly Timer twitchChatClientReconnect;

  /// <summary>
  ///   The lock to prevent mutual exclusion on the <see cref="client" /> object.
  /// </summary>
  private readonly object twitchClientLock = new();

  /// <summary>
  ///   The twitch client to send and receive messages with.
  /// </summary>
  private TwitchClient? client;

  /// <summary>
  ///   The callback(s) to invoke when a channel receives a chat message.
  /// </summary>
  private Dictionary<string, Action<OnMessageReceivedArgs>?> _onMessageReceived = new();

  /// <summary>
  ///   The callback(s) to invoke when a channel is raided.
  /// </summary>
  private Dictionary<string, Action<OnRaidNotificationArgs>?> onRaid = new();

  /// <summary>
  ///   The callback(s) to invoke when a channel receives a ban message.
  /// </summary>
  private Dictionary<string, Action<OnUserBannedArgs>?> onUserBanReceived = new();

  /// <summary>
  ///   The web socket to connect to twitch chat with.
  /// </summary>
  private WebSocketClient? socket;

  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchClientProxy" /> class.
  /// </summary>
  protected TwitchClientProxy() {
    joinedChannels = new HashSet<string>();

    // The timer for checking to make sure the IRC channel is connected.
    twitchChatClientReconnect = new Timer(1000);
    twitchChatClientReconnect.AutoReset = false;
    twitchChatClientReconnect.Elapsed += TwitchChatClientReconnectOnElapsed;
    twitchChatClientReconnect.Start();
  }

  /// <summary>
  ///   The singleton instance of the <see cref="TwitchClientProxy" /> class.
  /// </summary>
  public static TwitchClientProxy Instance {
    get {
      if (null == instance) {
        instance = new TwitchClientProxy();
      }

      return instance;
    }
  }

  /// <inheritdoc />
  public string? TwitchUsername { get; set; }

  /// <inheritdoc />
  public string? TwitchOAuthToken { get; set; }

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
      lock (twitchClientLock) {
        // If we are not connected for some reason, we shouldn't have gotten here, so get out.
        if (null == client ||
            !client.IsConnected ||
            null == client.JoinedChannels.FirstOrDefault(j =>
              channel.Equals(j.Channel, StringComparison.InvariantCultureIgnoreCase))) {
          return false;
        }

        LOG.Info($"{channel} Sending: {message}");
        client.SendMessage(channel, message);
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
    var channelSan = channel.ToLowerInvariant();
    lock (_onMessageReceived) {
      _onMessageReceived[channelSan] = callback;
    }
  }

  /// <inheritdoc />
  public void RemoveMessageCallback(string channel, Action<OnMessageReceivedArgs> callback) {
    bool shouldRemove = false;
    var channelSan = channel.ToLowerInvariant();
    lock (_onMessageReceived) {
      _onMessageReceived.Remove(channel);
    }

    if (shouldRemove) {
      client?.LeaveChannel(channelSan);
      
      // First add the channel to the master list.
      lock (joinedChannels) {
        joinedChannels.Add(channelSan);
      }
    }
  }

  /// <inheritdoc />
  public async Task AddBannedCallback(string channel, Action<OnUserBannedArgs> callback) {
    await JoinChannel(channel);

    lock (onUserBanReceived) {
      onUserBanReceived[channel] = callback;
    }
  }

  /// <inheritdoc />
  public void RemoveBannedCallback(string channel, Action<OnUserBannedArgs> callback) {
    lock (onUserBanReceived) {
      onUserBanReceived.Remove(channel);
    }
  }

  /// <inheritdoc />
  public async Task AddRaidCallback(string channel, Action<OnRaidNotificationArgs> callback) {
    await JoinChannel(channel);

    lock (onRaid) {
      onRaid[channel] = callback;
    }
  }

  /// <inheritdoc />
  public void RemoveRaidCallback(string channel, Action<OnRaidNotificationArgs> callback) {
    lock (onRaid) {
      onRaid.Remove(channel);
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
    lock (joinedChannels) {
      joinedChannels.Add(channel.ToLowerInvariant());
    }

    // Try to connect.
    if (!await Connect()) {
      return false;
    }

    return await Task.Run(() => {
      try {
        // If we don't have a client, give up.
        if (null == client) {
          return false;
        }

        lock (twitchClientLock) {
          // If we are already in the channel, we are done.
          if (null != client.JoinedChannels.FirstOrDefault(c =>
                channel.Equals(c.Channel, StringComparison.InvariantCultureIgnoreCase))) {
            return true;
          }

          // Otherwise, join the channel. At one point we waited here on the "OnJoinedChannel" to ensure the
          // connection before moving onto the next channel. However, it was causing a massive slowdown in
          // the application, and we've been working fine without it...so for now...we try this...
          client.JoinChannel(channel);
        }

        return true;
      }
      catch {
        return false;
      }
    });
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
    lock (joinedChannels) {
      allChannels = joinedChannels.ToArray();
    }

    // Join all the channels.
    foreach (string channel in allChannels) {
      await JoinChannel(channel);
    }

    // Restart the timer.
    twitchChatClientReconnect.Start();
  }

  /// <summary>
  ///   Connects to twitch chat.
  /// </summary>
  /// <returns>True if successful, false otherwise.</returns>
  private async Task<bool> Connect() {
    // If we're already connected, we are good to go.
    lock (twitchClientLock) {
      if (client?.IsConnected ?? false) {
        return true;
      }
    }

    // If we don't have the ability to connect, we can leave early.
    if (string.IsNullOrWhiteSpace(TwitchUsername) || string.IsNullOrWhiteSpace(TwitchOAuthToken)) {
      return false;
    }

    return await Task.Run(() => {
      try {
        bool isConnected = false;
        lock (twitchClientLock) {
          if (client?.IsConnected ?? false) {
            return true;
          }
          
          // If this is a first time initialization, create a brand-new client.
          bool haveNoClient = null == client;
          if (haveNoClient) {
            var clientOptions = new ClientOptions
              { MessagesAllowedInPeriod = 750, ThrottlingPeriod = TimeSpan.FromSeconds(30) };

            socket = new WebSocketClient(clientOptions);
            client = new TwitchClient(socket);
            var credentials = new ConnectionCredentials(TwitchUsername, TwitchOAuthToken);
            client.Initialize(credentials);
            client.AutoReListenOnException = true;
            client.OnMessageReceived += TwitchChatClient_OnMessageReceived;
            client.OnUserBanned += TwitchChatClient_OnUserBanned;
            client.OnRaidNotification += TwitchChatClient_OnRaidNotification;
            client.OnDisconnected += (sender, args) => {
              LOG.Error("Twitch Client Disconnected");
            };
            client.OnConnectionError += (sender, args) => {
              LOG.Error($"Twitch Client Connection Error: {args.Error.Message}");
            };
            client.OnError += (sender, args) => {
              LOG.Error($"Twitch Client Error: {args.Exception.Message}");
            };
            client.OnIncorrectLogin += (sender, args) => {
              LOG.Error($"Twitch Client Incorrect Login: {args.Exception.Message}");
            };
            client.OnNoPermissionError += (sender, args) => {
              LOG.Error("Twitch Client No Permission Error");
            };
          }

          try {
            // If we are not connect, connect.
            if (null != client && !client.IsConnected) {
              // If this is a new chat client, connect for the first time, otherwise reconnect.
              Action connect = haveNoClient ? () => client.Connect() : () => client.Reconnect();
              using var connectedEvent = new ManualResetEventSlim(false);
              EventHandler<OnConnectedArgs> onConnected = (_, _) => connectedEvent.Set();
              EventHandler<OnReconnectedEventArgs> onReconnect = (_, _) => connectedEvent.Set();
              try {
                client!.OnConnected += onConnected;
                client!.OnReconnected += onReconnect;
                connect();
                if (!connectedEvent.Wait(30 * 1000)) {
                  return false;
                }
              }
              finally {
                client.OnConnected -= onConnected;
                client.OnReconnected -= onReconnect;
              }
            }
          }
          catch {
          }

          // Determine if we successfully connected.
          isConnected = client?.IsConnected ?? false;
        }

        return isConnected;
      }
      catch {
        return false;
      }
    });
  }

  /// <summary>
  ///   Handles when the channel receives a raid.
  /// </summary>
  /// <param name="sender">The twitch client.</param>
  /// <param name="e">The event arguments.</param>
  private void TwitchChatClient_OnRaidNotification(object? sender, OnRaidNotificationArgs e) {
    Action<OnRaidNotificationArgs>? callback;
    var channel = e.Channel.ToLowerInvariant();
    lock (onRaid) {
      onRaid.TryGetValue(channel, out callback);
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
    var channelSan = e.ChatMessage.Channel.ToLowerInvariant();
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
    var channel = e.UserBan.Channel.ToLowerInvariant();
    lock (onUserBanReceived) {
      onUserBanReceived.TryGetValue(channel, out callback);
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
      client?.Disconnect();
      twitchChatClientReconnect?.Dispose();
      socket?.Dispose();
    }

    instance = null;
  }
}