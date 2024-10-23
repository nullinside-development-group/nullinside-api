using System.Security.Cryptography;

namespace Nullinside.Api.Common.Auth;

/// <summary>
///   Random utilities for authentication.
/// </summary>
public static class AuthUtils {
  /// <summary>
  ///   Generates a new unique bearer token.
  /// </summary>
  /// <returns>A bearer token.</returns>
  public static string GenerateBearerToken() {
    // This method is trash but it doesn't matter. We should be doing real OAuth tokens with expirations and
    // renewals. Right now nothing that exists on the site requires this level of sophistication.
    string allowed = "ABCDEFGHIJKLMONOPQRSTUVWXYZabcdefghijklmonopqrstuvwxyz0123456789";
    int strlen = 255; // Or whatever
    char[] randomChars = new char[strlen];

    for (int i = 0; i < strlen; i++) {
      randomChars[i] = allowed[RandomNumberGenerator.GetInt32(0, allowed.Length)];
    }

    return new string(randomChars);
  }
}