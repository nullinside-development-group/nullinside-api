namespace Nullinside.Api.Common;

/// <summary>
///   Extension methods for <see cref="DateTime" />.
/// </summary>
public static class DateTimeExtensions {
  /// <summary>
  ///   Converts unix timestamp to a <see cref="DateTime" />.
  /// </summary>
  /// <param name="unixTimestamp">The unix timestamp.</param>
  /// <returns>The DateTime representation of the unix timestamp.</returns>
  public static DateTime FromUnixTimestamp(double unixTimestamp) {
    // Unix timestamp is seconds past epoch
    var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
    dateTime = dateTime.AddSeconds(unixTimestamp / 1000d).ToLocalTime();
    return dateTime;
  }
}