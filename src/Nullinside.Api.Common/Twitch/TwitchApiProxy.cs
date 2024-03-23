using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace Nullinside.Api.Common.Twitch;

/// <summary>
///   A proxy for making twitch requests.
/// </summary>
public class TwitchApiProxy {
  /// <summary>
  ///   The, public, twitch client id.
  /// </summary>
  public static string ClientId { get; } = Environment.GetEnvironmentVariable("TWITCH_BOT_CLIENT_ID")!;

  /// <summary>
  ///   The, private, twitch client secret.
  /// </summary>
  public static string ClientSecret { get; } = Environment.GetEnvironmentVariable("TWITCH_BOT_CLIENT_SECRET")!;

  /// <summary>
  ///   The redirect url.
  /// </summary>
  public static string ClientRedirect { get; } = Environment.GetEnvironmentVariable("TWITCH_BOT_CLIENT_REDIRECT")!;

  /// <summary>
  ///   The Twitch access token.
  /// </summary>
  public string? AccessToken { get; set; }

  /// <summary>
  ///   The refresh token.
  /// </summary>
  public string? RefreshToken { get; set; }

  /// <summary>
  ///   The UTC <see cref="DateTime" /> when the <see cref="AccessToken" /> expires.
  /// </summary>
  public DateTime? ExpiresUtc { get; set; }

  /// <summary>
  ///   The number of times to retry queries before giving up.
  /// </summary>
  public int Retries { get; set; } = 3;

  /// <summary>
  ///   Gets a new instance of the <see cref="TwitchAPI" />.
  /// </summary>
  /// <returns>A new instance of the <see cref="TwitchAPI" />.</returns>
  private TwitchAPI GetApi() {
    var api = new TwitchAPI {
      Settings = {
        ClientId = ClientId,
        AccessToken = AccessToken
      }
    };
    return api;
  }

  /// <summary>
  ///   Refreshes the access token.
  /// </summary>
  /// <param name="token">The cancellation token.</param>
  /// <remarks>The object will have it's properties updated with the new settings for the token.</remarks>
  /// <returns>True if successful, false otherwise.</returns>
  public async Task<bool> RefreshTokenAsync(CancellationToken token = new()) {
    try {
      TwitchAPI api = GetApi();
      RefreshResponse? response = await api.Auth.RefreshAuthTokenAsync(RefreshToken, ClientSecret, ClientId);
      if (null == response) {
        return false;
      }

      AccessToken = response.AccessToken;
      RefreshToken = response.RefreshToken;
      ExpiresUtc = DateTime.UtcNow + TimeSpan.FromSeconds(response.ExpiresIn);
      return true;
    }
    catch (Exception) {
      return false;
    }
  }

  /// <summary>
  ///   Gets a new access token.
  /// </summary>
  /// <param name="code">The code to send to twitch to generate a new access token.</param>
  /// <param name="token">The cancellation token.</param>
  /// <remarks>The object will have it's properties updated with the new settings for the token.</remarks>
  /// <returns>True if successful, false otherwise.</returns>
  public async Task<bool> GetAccessToken(string code, CancellationToken token = new()) {
    TwitchAPI api = GetApi();
    AuthCodeResponse? response = await api.Auth.GetAccessTokenFromCodeAsync(code, ClientSecret, ClientRedirect);
    if (null == response) {
      return false;
    }

    AccessToken = response.AccessToken;
    RefreshToken = response.RefreshToken;
    ExpiresUtc = DateTime.UtcNow + TimeSpan.FromSeconds(response.ExpiresIn);
    return true;
  }

  /// <summary>
  ///   Gets the email address of the owner of the <see cref="AccessToken" />.
  /// </summary>
  /// <param name="token">The cancellation token.</param>
  /// <returns>The email address if successful, null otherwise.</returns>
  public async Task<string?> GetUserEmail(CancellationToken token = new()) {
    return await Retry.Execute(async () => {
      TwitchAPI api = GetApi();
      GetUsersResponse? response = await api.Helix.Users.GetUsersAsync();
      if (null == response) {
        return null;
      }

      return response.Users.FirstOrDefault()?.Email;
    }, Retries, token);
  }
}