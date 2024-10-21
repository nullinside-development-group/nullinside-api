using System.Security.Cryptography;

using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Common;
using Nullinside.Api.Model.Ddl;

namespace Nullinside.Api.Model.Shared;

/// <summary>
///   Helper methods for user functions in the database.
/// </summary>
public static class UserHelpers {
  /// <summary>
  ///   Generates a new bearer token, saves it to the database, and returns it.
  /// </summary>
  /// <param name="dbContext">The database context.</param>
  /// <param name="email">The email address of the user, user will be created if they don't already exist.</param>
  /// <param name="token">The cancellation token.</param>
  /// <param name="authToken">The authorization token for twitch, if applicable.</param>
  /// <param name="refreshToken">The refresh token for twitch, if applicable.</param>
  /// <param name="expires">The expiration date of the token for twitch, if applicable.</param>
  /// <param name="twitchUsername">The username of the user on twitch.</param>
  /// <param name="twitchId">The id of the user on twitch.</param>
  /// <returns>The bearer token if successful, null otherwise.</returns>
  public static async Task<string?> GetTokenAndSaveToDatabase(INullinsideContext dbContext, string email,
    CancellationToken token = new(), string? authToken = null, string? refreshToken = null, DateTime? expires = null,
    string? twitchUsername = null, string? twitchId = null) {
    string bearerToken = GenerateBearerToken();
    try {
      // We prevent banned users from logging into the site.
      User? existing = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsBanned, token);
      if (null == existing) {
        dbContext.Users.Add(new User {
          Email = email,
          Token = bearerToken,
          TwitchId = twitchId,
          TwitchUsername = twitchUsername,
          TwitchToken = authToken,
          TwitchRefreshToken = refreshToken,
          TwitchTokenExpiration = expires,
          UpdatedOn = DateTime.UtcNow,
          CreatedOn = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(token);

        existing = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, token);
        if (null == existing) {
          return null;
        }

        dbContext.UserRoles.Add(new UserRole {
          Role = UserRoles.User,
          UserId = existing.Id,
          RoleAdded = DateTime.UtcNow
        });
      }
      else {
        existing.Token = bearerToken;
        existing.TwitchId = twitchId;
        existing.TwitchUsername = twitchUsername;
        existing.TwitchToken = authToken;
        existing.TwitchRefreshToken = refreshToken;
        existing.TwitchTokenExpiration = expires;
        existing.UpdatedOn = DateTime.UtcNow;
      }

      await dbContext.SaveChangesAsync(token);
      return bearerToken;
    }
    catch {
      return null;
    }
  }

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