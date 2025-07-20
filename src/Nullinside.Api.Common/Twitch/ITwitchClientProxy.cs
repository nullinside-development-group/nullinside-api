using TwitchLib.Client.Events;

namespace Nullinside.Api.Common.Twitch;

/// <summary>
///   Represents a twitch chat messaging client.
/// </summary>
public interface ITwitchClientProxy : IDisposable, IAsyncDisposable {
  /// <summary>
  ///   Gets or sets the twitch username to connect with.
  /// </summary>
  string? TwitchUsername { get; set; }

  /// <summary>
  ///   Gets or sets the twitch OAuth token to use to connect.
  /// </summary>
  string? TwitchOAuthToken { get; set; }

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
  Task<bool> SendMessage(string channel, string message, uint retryConnection = 5);

  /// <summary>
  ///   Adds a callback for when the channel receives a new chat message.
  /// </summary>
  /// <param name="channel">The name of the channel to add the callback for.</param>
  /// <param name="callback">The callback to invoke.</param>
  /// <returns>An asynchronous task.</returns>
  Task AddMessageCallback(string channel, Action<OnMessageReceivedArgs> callback);

  /// <summary>
  ///   Removes a callback for when the channel receives a new chat message.
  /// </summary>
  /// <param name="channel">The name of the channel to add the callback for.</param>
  /// <param name="callback">The callback to remove.</param>
  /// <returns>An asynchronous task.</returns>
  void RemoveMessageCallback(string channel, Action<OnMessageReceivedArgs> callback);

  /// <summary>
  ///   Adds a callback for when users are banned from the chat.
  /// </summary>
  /// <param name="channel">The channel to subscribe to notifications for.</param>
  /// <param name="callback">The callback to invoke when a user is banned.</param>
  Task AddBannedCallback(string channel, Action<OnUserBannedArgs> callback);

  /// <summary>
  ///   Removes a callback for when users are banned from the chat.
  /// </summary>
  /// <param name="channel">The name of the channel to add the callback for.</param>
  /// <param name="callback">The callback to remove from when a user is banned.</param>
  void RemoveBannedCallback(string channel, Action<OnUserBannedArgs> callback);

  /// <summary>
  ///   Adds a callback for when the channel receives a raid.
  /// </summary>
  /// <param name="channel">The channel to subscribe to callbacks for.</param>
  /// <param name="callback">The callback to invoke.</param>
  /// <returns>An asynchronous task.</returns>
  Task AddRaidCallback(string channel, Action<OnRaidNotificationArgs> callback);

  /// <summary>
  ///   Removes a callback for when the channel receives a raid.
  /// </summary>
  /// <param name="channel">The name of the channel to add the callback for.</param>
  /// <param name="callback">The callback to remove.</param>
  /// <returns>An asynchronous task.</returns>
  void RemoveRaidCallback(string channel, Action<OnRaidNotificationArgs> callback);

  /// <summary>
  ///   Adds a callback for being disconnected from the twitch chat server.
  /// </summary>
  /// <param name="callback">The callback to invoke.</param>
  /// <returns>An asynchronous task.</returns>
  void AddDisconnectedCallback(Action callback);

  /// <summary>
  ///   Removes a callback for being disconnected from the twitch chat server.
  /// </summary>
  /// <param name="callback">The callback to remove.</param>
  /// <returns>An asynchronous task.</returns>
  void RemoveDisconnectedCallback(Action callback);
}