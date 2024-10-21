using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

using Nullinside.Api.Model.Ddl;

namespace Nullinside.Api.Model;

/// <summary>
/// Represents the nullinside database.
/// </summary>
public interface INullinsideContext {
  /// <summary>
  ///   The users table which contains all of the users that have ever authenticated with the site.
  /// </summary>
  public DbSet<User> Users { get; set; }

  /// <summary>
  ///   The user's roles table which contains all of the "roles" the user has in the application.
  /// </summary>
  public DbSet<UserRole> UserRoles { get; set; }

  /// <summary>
  ///   The docker deployments that are configurable in the applications.
  /// </summary>
  public DbSet<DockerDeployments> DockerDeployments { get; set; }

  /// <summary>
  ///   The docker deployments that are configurable in the applications.
  /// </summary>
  public DbSet<TwitchUser> TwitchUser { get; set; }

  /// <summary>
  ///   The docker deployments that are configurable in the applications.
  /// </summary>
  public DbSet<TwitchBan> TwitchBan { get; set; }

  /// <summary>
  ///   The feature toggles.
  /// </summary>
  public DbSet<FeatureToggle> FeatureToggle { get; set; }

  /// <summary>
  ///   The twitch user configuration.
  /// </summary>
  public DbSet<TwitchUserConfig> TwitchUserConfig { get; set; }

  /// <summary>
  ///   The twitch logs of users banned outside the bot.
  /// </summary>
  public DbSet<TwitchUserBannedOutsideOfBotLogs> TwitchUserBannedOutsideOfBotLogs { get; set; }

  /// <summary>
  ///   The twitch logs of the user's chat.
  /// </summary>
  public DbSet<TwitchUserChatLogs> TwitchUserChatLogs { get; set; }

  /// <summary>
  ///   Provides access to database related information and operations for this context.
  /// </summary>
  public DatabaseFacade Database { get; }

  /// <summary>
  ///     Saves all changes made in this context to the database.
  /// </summary>
  /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
  /// <returns>
  ///     A task that represents the asynchronous save operation. The task result contains the
  ///     number of state entries written to the database.
  /// </returns>
  public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}