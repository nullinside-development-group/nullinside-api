using System.Timers;

using log4net;

using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace Nullinside.Api.Common.Twitch;

using Timer = System.Timers.Timer;

/// <summary>
///   The singleton of the Cathy bot that existing in chat for reading and sending twitch chat messages.
/// </summary>
public class TwitchClientProxy : IDisposable {
  /// <summary>
  ///   The logger.
  /// </summary>
  private static readonly ILog LOG = LogManager.GetLogger(typeof(TwitchClientProxy));

  /// <summary>
  ///   The singleton instance of the class.
  /// </summary>
  private static TwitchClientProxy? instance;

  /// <summary>
  ///   The lock to prevent mutual exclusion on <see cref="onMessageReceived" />
  ///   and <see cref="onRaid" /> callbacks.
  /// </summary>
  private readonly object callbackLock = new object();

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
  private readonly object twitchClientLock = new object();

  /// <summary>
  ///   The twitch client to send and receive messages with.
  /// </summary>
  private TwitchClient? client;

  /// <summary>
  ///   The callback(s) to invoke when a channel receives a chat message.
  /// </summary>
  private Action<OnMessageReceivedArgs>? onMessageReceived;

  /// <summary>
  ///   The callback(s) to invoke when a channel receives a ban message.
  /// </summary>
  private Action<OnUserBannedArgs>? onUserBanReceived;

  /// <summary>
  ///   The callback(s) to invoke when a channel is raided.
  /// </summary>
  private Action<OnRaidNotificationArgs>? onRaid;

  /// <summary>
  ///   The web socket to connect to twitch chat with.
  /// </summary>
  private WebSocketClient? socket;

  /// <summary>
  ///   Initializes a new instance of the <see cref="TwitchClientProxy" /> class.
  /// </summary>
  protected TwitchClientProxy() {
    this.joinedChannels = new HashSet<string>();

    // The timer for checking to make sure the IRC channel is connected.
    this.twitchChatClientReconnect = new Timer(1000);
    this.twitchChatClientReconnect.AutoReset = false;
    this.twitchChatClientReconnect.Elapsed += this.TwitchChatClientReconnectOnElapsed;
    this.twitchChatClientReconnect.Start();
  }

  /// <summary>
  ///   The singleton instance of the <see cref="TwitchClientProxy" /> class.
  /// </summary>
  public static TwitchClientProxy Instance {
    get {
      if (null == TwitchClientProxy.instance) {
        TwitchClientProxy.instance = new TwitchClientProxy();
      }

      return TwitchClientProxy.instance;
    }
  }

  /// <summary>
  /// Gets or sets the twitch username to connect with.
  /// </summary>
  public string? TwitchUsername { get; set; }

  /// <summary>
  /// Gets or sets the twitch OAuth token to use to connect.
  /// </summary>
  public string? TwitchOAuthToken { get; set; }

  /// <summary>
  ///   Send a message in twitch chat.
  /// </summary>
  /// <param name="channel">The channel to send the message in.</param>
  /// <param name="message">The message to send.</param>
  /// <param name="retryConnection">
  ///   The number of times to retry connecting to twitch chat before
  ///   giving up.
  /// </param>
  /// <returns>True if connected and sent, false otherwise.</returns>
  public async Task<bool> SendMessage(string channel, string message, uint retryConnection = 5) {
    // Sanity check.
    if (string.IsNullOrWhiteSpace(channel)) {
      return false;
    }

    // Try to connect and join the channel.
    var connectedAndJoined = false;
    for (var i = 0; i < retryConnection; i++) {
      if (await this.JoinChannel(channel)) {
        connectedAndJoined = true;
        break;
      }
    }

    // If we failed to connect and join the channel, give up.
    if (!connectedAndJoined) {
      return false;
    }

    try {
      lock (this.twitchClientLock) {
        // If we are not connected for some reason, we shouldn't have gotten here, so get out.
        if (null == this.client ||
            !this.client.IsConnected ||
            null == this.client.JoinedChannels.FirstOrDefault(j =>
              channel.Equals(j.Channel, StringComparison.InvariantCultureIgnoreCase))) {
          return false;
        }

        TwitchClientProxy.LOG.Info($"{channel} Sending: {message}");
        this.client.SendMessage(channel, message);
      }
    }
    catch {
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
    lock (this.joinedChannels) {
      this.joinedChannels.Add(channel.ToLowerInvariant());
    }

    // Try to connect.
    if (!await this.Connect()) {
      return false;
    }

    return await Task.Run(() => {
      try {
        // If we don't have a client, give up.
        if (null == this.client) {
          return false;
        }

        lock (this.twitchClientLock) {
          // If we are already in the channel, we are done.
          if (null != this.client.JoinedChannels.FirstOrDefault(c =>
                channel.Equals(c.Channel, StringComparison.InvariantCultureIgnoreCase))) {
            return true;
          }

          // Otherwise, join the channel. At one point we waited here on the "OnJoinedChannel" to ensure the
          // connection before moving onto the next channel. However, it was causing a massive slowdown in
          // the application, and we've been working fine without it...so for now...we try this...
          this.client.JoinChannel(channel);
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
  private async void TwitchChatClientReconnectOnElapsed(object sender, ElapsedEventArgs e) {
    // Connect the chat client.
    await this.Connect();

    // Pull the master list of channels we should be connected to the stack.
    string[]? allChannels = null;
    lock (this.joinedChannels) {
      allChannels = this.joinedChannels.ToArray();
    }

    // Join all the channels.
    foreach (var channel in allChannels) {
      await this.JoinChannel(channel);
    }

    // Restart the timer.
    this.twitchChatClientReconnect.Start();
  }

  /// <summary>
  ///   Connects to twitch chat.
  /// </summary>
  /// <returns>True if successful, false otherwise.</returns>
  private async Task<bool> Connect() {
    // If we're already connected, we are good to go.
    lock (this.twitchClientLock) {
      if (this.client?.IsConnected ?? false) {
        return true;
      }
    }

    // If we don't have the ability to connect, we can leave early.
    if (string.IsNullOrWhiteSpace(this.TwitchUsername) || string.IsNullOrWhiteSpace(this.TwitchOAuthToken)) {
      return false;
    }

    return await Task.Run(() => {
      try {
        bool isConnected = false;
        lock (this.twitchClientLock) {
          // If this is a first time initialization, create a brand-new client.
          bool haveNoClient = null == this.client;
          if (haveNoClient) {
            var clientOptions = new ClientOptions
              { MessagesAllowedInPeriod = 750, ThrottlingPeriod = TimeSpan.FromSeconds(30) };

            this.socket = new WebSocketClient(clientOptions);
            this.client = new TwitchClient(this.socket);
            var credentials = new ConnectionCredentials(this.TwitchUsername, this.TwitchOAuthToken);
            this.client.Initialize(credentials);
            this.client.AutoReListenOnException = true;
            this.client.OnMessageReceived += this.TwitchChatClient_OnMessageReceived;
            this.client.OnUserBanned += this.TwitchChatClient_OnUserBanned;
            this.client.OnRaidNotification += this.TwitchChatClient_OnRaidNotification;
          }

          try {
            // If we are not connect, connect.
            if (!this.client?.IsConnected ?? false) {
              // If this is a new chat client, connect for the first time, otherwise reconnect.
              Action connect = haveNoClient ? () => this.client.Connect() : () => this.client.Reconnect();
              using var connectedEvent = new ManualResetEventSlim(false);
              EventHandler<OnConnectedArgs> onConnected = (_, _) => connectedEvent.Set();
              try {
                this.client!.OnConnected += onConnected;
                connect();
                if (!connectedEvent.Wait(30 * 1000)) {
                  return false;
                }
              }
              finally {
                this.client.OnConnected -= onConnected;
              }
            }
          }
          catch {
          }

          // Determine if we successfully connected.
          isConnected = this.client?.IsConnected ?? false;
        }

        return isConnected;
      }
      catch {
        return false;
      }
    });
  }

  /// <summary>
  ///   Adds a callback for when the channel receives a new chat message.
  /// </summary>
  /// <param name="channel">The name of the channel to add the callback for.</param>
  /// <param name="callback">The callback to invoke.</param>
  /// <returns>An asynchronous task.</returns>
  public async Task AddMessageCallback(string channel, Action<OnMessageReceivedArgs> callback) {
    await this.JoinChannel(channel);

    lock (this.callbackLock) {
      this.onMessageReceived -= callback;
      this.onMessageReceived += callback;
    }
  }

  /// <summary>
  ///   Removes a callback for when the channel receives a new chat message.
  /// </summary>
  /// <param name="callback">The callback to remove.</param>
  /// <returns>An asynchronous task.</returns>
  public void RemoveMessageCallback(Action<OnMessageReceivedArgs> callback) {
    lock (this.callbackLock) {
      this.onMessageReceived -= callback;
    }
  }

  /// <summary>
  /// Adds a callback for when users are banned from the chat.
  /// </summary>
  /// <param name="channel">The channel to subscribe to notifications for.</param>
  /// <param name="callback">The callback to invoke when a user is banned.</param>
  public async Task AddBannedCallback(string channel, Action<OnUserBannedArgs> callback) {
    await this.JoinChannel(channel);

    lock (this.callbackLock) {
      this.onUserBanReceived -= callback;
      this.onUserBanReceived += callback;
    }
  }

  /// <summary>
  /// Removes a callback for when users are banned from the chat.
  /// </summary>
  /// <param name="callback">The callback to remove from when a user is banned.</param>
  public void RemoveBannedCallback(Action<OnUserBannedArgs> callback) {
    lock (this.callbackLock) {
      this.onUserBanReceived -= callback;
    }
  }

  /// <summary>
  ///   Adds a callback for when the channel receives a raid.
  /// </summary>
  /// <param name="channel">The channel to subscribe to callbacks for.</param>
  /// <param name="callback">The callback to invoke.</param>
  /// <returns>An asynchronous task.</returns>
  public async Task AddRaidCallback(string channel, Action<OnRaidNotificationArgs> callback) {
    await this.JoinChannel(channel);

    lock (this.callbackLock) {
      this.onRaid -= callback;
      this.onRaid += callback;
    }
  }

  /// <summary>
  ///   Removes a callback for when the channel receives a raid.
  /// </summary>
  /// <param name="callback">The callback to remove.</param>
  /// <returns>An asynchronous task.</returns>
  public void RemoveRaidCallback(Action<OnRaidNotificationArgs> callback) {
    lock (this.callbackLock) {
      this.onRaid -= callback;
    }
  }

  /// <summary>
  ///   Handles when the channel receives a raid.
  /// </summary>
  /// <param name="sender">The twitch client.</param>
  /// <param name="e">The event arguments.</param>
  private void TwitchChatClient_OnRaidNotification(object? sender, OnRaidNotificationArgs e) {
    Delegate[]? invokeList = null;

    lock (this.callbackLock) {
      invokeList = this.onRaid?.GetInvocationList();
    }

    if (null == invokeList) {
      return;
    }

    foreach (var del in invokeList) {
      del.DynamicInvoke(e);
    }
  }

  /// <summary>
  ///   Handles when the channel receives a new chat message.
  /// </summary>
  /// <param name="sender">The twitch client.</param>
  /// <param name="e">The event arguments.</param>
  private void TwitchChatClient_OnMessageReceived(object? sender, OnMessageReceivedArgs e) {
    Delegate[]? invokeList = null;

    lock (this.callbackLock) {
      invokeList = this.onMessageReceived?.GetInvocationList();
    }

    if (null == invokeList) {
      return;
    }

    foreach (var del in invokeList) {
      del.DynamicInvoke(e);
    }
  }

  private void TwitchChatClient_OnUserBanned(object? sender, OnUserBannedArgs e) {
    Delegate[]? invokeList = null;

    lock (this.callbackLock) {
      invokeList = this.onUserBanReceived?.GetInvocationList();
    }

    if (null == invokeList) {
      return;
    }

    foreach (var del in invokeList) {
      del.DynamicInvoke(e);
    }
  }

  /// <summary>
  /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
  /// </summary>
  /// <param name="disposing">True if called directly, false if called from the destructor.</param>
  protected virtual void Dispose(bool disposing) {
    if (disposing) {
      this.twitchChatClientReconnect?.Dispose();
      this.socket?.Dispose();
    }
  }

  /// <summary>
  /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
  /// </summary>
  public void Dispose() {
    Dispose(true);
    GC.SuppressFinalize(this);
  }
}