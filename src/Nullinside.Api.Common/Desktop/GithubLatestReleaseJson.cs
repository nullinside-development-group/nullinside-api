﻿// ReSharper disable All

using System.Diagnostics.CodeAnalysis;

namespace Nullinside.Api.Common.Desktop;

/// <summary>
///   The response information from GitHub's API.
/// </summary>
[ExcludeFromCodeCoverage]
public class GithubLatestReleaseJson {
  /// <summary>
  ///   The url of the resource.
  /// </summary>
  public string? html_url { get; set; }

  /// <summary>
  ///   The name of the release.
  /// </summary>
  public string? name { get; set; }
}