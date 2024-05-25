namespace Nullinside.Api.Common;

/// <summary>
/// Constant strings used throughout the application.
/// </summary>
public static class Constants {
  /// <summary>
  /// The API for getting the latest release version number.
  /// </summary>
  public const string APP_UPDATE_API = "https://api.github.com/repos/{0}/{1}/releases/latest";

  /// <summary>
  /// The user agent a browser would use in its headers.
  /// </summary>
  public const string FAKE_USER_AGENT =
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36";
}