﻿// ReSharper disable All

using System.Diagnostics.CodeAnalysis;

namespace Nullinside.Api.Common.Twitch.Json;

/// <summary>
///   The response to a query for what channels a user is moderator for.
/// </summary>
[ExcludeFromCodeCoverage]
public class TwitchModeratedChannelsResponse {
  /// <summary>
  ///   The list of channels the user moderates for.
  /// </summary>
  public List<TwitchModeratedChannel> data { get; set; } = null!;

  /// <summary>
  ///   The pagination.
  /// </summary>
  public Pagination pagination { get; set; } = null!;
}

/// <summary>
///   A channel the user moderates.
/// </summary>
[ExcludeFromCodeCoverage]
public class TwitchModeratedChannel {
  /// <summary>
  ///   The twitch id.
  /// </summary>
  public string broadcaster_id { get; set; } = null!;

  /// <summary>
  ///   The twitch login.
  /// </summary>
  public string broadcaster_login { get; set; } = null!;

  /// <summary>
  ///   The twitch username.
  /// </summary>
  public string broadcaster_name { get; set; } = null!;
}

/// <summary>
///   Pagination information.
/// </summary>
[ExcludeFromCodeCoverage]
public class Pagination {
  /// <summary>
  ///   The cursor to pass to "after" for pagination.
  /// </summary>
  public string? cursor { get; set; }
}