using System.Security.Cryptography;

namespace Nullinside.Api.Common.Auth;

/// <summary>
///   Random utilities for authentication.
/// </summary>
public static class AuthUtils {
  /// <summary>
  ///   Generates a new unique token.
  /// </summary>
  /// <returns>A token.</returns>
  public static string GenerateToken() {
    string allowed = "ABCDEFGHIJKLMONOPQRSTUVWXYZabcdefghijklmonopqrstuvwxyz0123456789";
    int strlen = 255; // Or whatever
    char[] randomChars = new char[strlen];

    for (int i = 0; i < strlen; i++) {
      randomChars[i] = allowed[RandomNumberGenerator.GetInt32(0, allowed.Length)];
    }

    return new string(randomChars);
  }
}