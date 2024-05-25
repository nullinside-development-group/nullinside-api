using System.Net;

using Newtonsoft.Json;

namespace Nullinside.Api.Common.Desktop;

/// <summary>
///   Handles checking for updates to the application via GitHub releases.
/// </summary>
public class GitHubUpdateManager {
  /// <summary>
  ///   Gets the latest version number of the release.
  /// </summary>
  /// <param name="owner">The owner of the repository.</param>
  /// <param name="repo">The repository name.</param>
  /// <returns>The response from GitHub.</returns>
  public static async Task<GithubLatestReleaseJson?> GetLatestVersion(string owner, string repo) {
    if (string.IsNullOrWhiteSpace(owner)) {
      throw new ArgumentException("Cannot be null or whitespace", nameof(owner));
    }
    
    if (string.IsNullOrWhiteSpace(repo)) {
      throw new ArgumentException("Cannot be null or whitespace", nameof(repo));
    }
    
    var handler = new HttpClientHandler();
    handler.AutomaticDecompression = ~DecompressionMethods.None;
    using var httpClient = new HttpClient(handler);
    using var request = new HttpRequestMessage(HttpMethod.Get, string.Format(Constants.APP_UPDATE_API, owner, repo));
    request.Headers.TryAddWithoutValidation("user-agent", Constants.FAKE_USER_AGENT);
    HttpResponseMessage response = await httpClient.SendAsync(request);
    if (!response.IsSuccessStatusCode) {
      return null;
    }

    string body = await response.Content.ReadAsStringAsync();
    return JsonConvert.DeserializeObject<GithubLatestReleaseJson>(body);
  }
}