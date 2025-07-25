﻿using System.Diagnostics;
using System.IO.Compression;
using System.Net;

using log4net;

using Microsoft.VisualBasic.FileIO;

using Newtonsoft.Json;

namespace Nullinside.Api.Common.Desktop;

/// <summary>
///   Handles checking for updates to the application via GitHub releases.
/// </summary>
public static class GitHubUpdateManager {
  /// <summary>
  ///   The logger.
  /// </summary>
  private static readonly ILog Log = LogManager.GetLogger(typeof(GitHubUpdateManager));

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
    HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);
    if (!response.IsSuccessStatusCode) {
      return null;
    }

    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    return JsonConvert.DeserializeObject<GithubLatestReleaseJson>(body);
  }

  /// <summary>
  ///   Prepares to update this application before this application is closed.
  /// </summary>
  public static async Task PrepareUpdate() {
    try {
      // To prepare the update, we just need to back up our files
      string backupFolder = Path.Combine(AppContext.BaseDirectory, "..", "backup");
      await DeleteFolderRetry(backupFolder).ConfigureAwait(false);

      Directory.CreateDirectory(backupFolder);
      FileSystem.CopyDirectory(AppContext.BaseDirectory, backupFolder);
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
  }

  /// <summary>
  ///   Runs this application from the backup folder to initiate the update on the installed folder.
  /// </summary>
  public static void ExitApplicationToUpdate() {
    try {
      // Since we have a backup folder from PrepareUpdate() we can just run the backup executable
      string backupFolder = Path.Combine(AppContext.BaseDirectory, "..", "backup");
      if (!Directory.Exists(backupFolder)) {
        return;
      }

      string ourExecutable = $"{AppDomain.CurrentDomain.FriendlyName}.exe";

      // we must pass the installation folder to the executable so it knows where to install
      Process.Start(Path.Combine(backupFolder, ourExecutable), $"--update \"{AppContext.BaseDirectory}\"");
      Environment.Exit(0);
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
  }

  /// <summary>
  ///   Performs the application update. This involves downloading the latest release from GitHub, extracting its contents
  ///   to the installation folder, and closing the currently running application while running the new one.
  /// </summary>
  public static async Task PerformUpdateAndRestart(string owner, string repo, string installFolder, string assetName) {
    try {
      // Delete the old install folder.
      await DeleteFolderContentsRetry(installFolder).ConfigureAwait(false);

      // Get the latest version of the application from GitHub.
      string zipLocation = Path.Combine(AppContext.BaseDirectory, assetName);
      GithubLatestReleaseJson? latestVersion = await GetLatestVersion(owner, repo).ConfigureAwait(false);
      using (var client = new HttpClient()) {
        using HttpResponseMessage response = await client.GetAsync($"https://github.com/{owner}/{repo}/releases/download/{latestVersion?.name}/{assetName}").ConfigureAwait(false);
        await using Stream streamToReadFrom = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        await using var fileStream = new FileStream(zipLocation, FileMode.Create);
        await streamToReadFrom.CopyToAsync(fileStream).ConfigureAwait(false);
      }

      // Extract the zip file to the installation folder.
      ZipFile.ExtractToDirectory(zipLocation, installFolder);

      // Run the new version of the application.
      Process.Start(Path.Combine(installFolder, $"{AppDomain.CurrentDomain.FriendlyName}.exe"), "--justUpdated");

      // Close this version of the application.
      Environment.Exit(0);
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
  }

  /// <summary>
  ///   Cleans up the previous update's files.
  /// </summary>
  public static async Task CleanupUpdate() {
    string backupFolder = Path.Combine(AppContext.BaseDirectory, "..", "backup");

    await DeleteFolderRetry(backupFolder).ConfigureAwait(false);
  }

  /// <summary>
  ///   Retries deleting a folder multiple times.
  /// </summary>
  /// <param name="folder">The folder to delete.</param>
  private static async Task DeleteFolderRetry(string folder) {
    try {
      await Retry.Execute(() => {
        if (Directory.Exists(folder)) {
          Directory.Delete(folder, true);
        }

        return Task.FromResult(true);
      }, 30, waitTime: TimeSpan.FromSeconds(1)).ConfigureAwait(false);
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
  }

  /// <summary>
  ///   Retries deleting the contents of a folder multiple times.
  /// </summary>
  /// <param name="folder">The folder to delete the contents of.</param>
  private static async Task DeleteFolderContentsRetry(string folder) {
    try {
      await Retry.Execute(() => {
        if (!Directory.Exists(folder)) {
          return Task.FromResult(true);
        }

        foreach (string file in Directory.GetFiles(folder)) {
          File.Delete(file);
        }

        foreach (string directory in Directory.GetDirectories(folder)) {
          Directory.Delete(directory, true);
        }

        return Task.FromResult(true);
      }, 30, waitTime: TimeSpan.FromSeconds(1)).ConfigureAwait(false);
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
  }
}