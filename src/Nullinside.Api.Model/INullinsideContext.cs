using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

using Nullinside.Api.Model.Ddl;

namespace Nullinside.Api.Model;

/// <summary>
///   Represents the nullinside database.
/// </summary>
public interface INullinsideContext : IAsyncDisposable {
  /// <summary>
  ///   The users table which contains all of the users that have ever authenticated with the site.
  /// </summary>
  DbSet<User> Users { get; set; }

  /// <summary>
  ///   The user's roles table which contains all of the "roles" the user has in the application.
  /// </summary>
  DbSet<UserRole> UserRoles { get; set; }

  /// <summary>
  ///   The docker deployments that are configurable in the applications.
  /// </summary>
  DbSet<DockerDeployments> DockerDeployments { get; set; }

  /// <summary>
  ///   The docker deployments that are configurable in the applications.
  /// </summary>
  DbSet<TwitchUser> TwitchUser { get; set; }

  /// <summary>
  ///   The docker deployments that are configurable in the applications.
  /// </summary>
  DbSet<TwitchBan> TwitchBan { get; set; }

  /// <summary>
  ///   The feature toggles.
  /// </summary>
  DbSet<FeatureToggle> FeatureToggle { get; set; }

  /// <summary>
  ///   The twitch user configuration.
  /// </summary>
  DbSet<TwitchUserConfig> TwitchUserConfig { get; set; }

  /// <summary>
  ///   The twitch logs of users banned outside the bot.
  /// </summary>
  DbSet<TwitchUserBannedOutsideOfBotLogs> TwitchUserBannedOutsideOfBotLogs { get; set; }

  /// <summary>
  ///   The twitch logs of the user's chat.
  /// </summary>
  DbSet<TwitchUserChatLogs> TwitchUserChatLogs { get; set; }

  /// <summary>
  ///   Provides access to database related information and operations for this context.
  /// </summary>
  DatabaseFacade Database { get; }

  /// <summary>
  ///   Saves all changes made in this context to the database.
  /// </summary>
  /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
  /// <returns>
  ///   A task that represents the asynchronous save operation. The task result contains the
  ///   number of state entries written to the database.
  /// </returns>
  Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

  /// <summary>
  ///   Saves all changes made in this context to the database.
  /// </summary>
  /// <returns>
  ///   The number of state entries written to the database.
  /// </returns>
  int SaveChanges();

  /// <summary>
  ///     Gets an <see cref="EntityEntry" /> for the given entity. The entry provides
  ///     access to change tracking information and operations for the entity.
  /// </summary>
  /// <remarks>
  ///     <para>
  ///         This method may be called on an entity that is not tracked. You can then
  ///         set the <see cref="EntityEntry.State" /> property on the returned entry
  ///         to have the context begin tracking the entity in the specified state.
  ///     </para>
  ///     <para>
  ///         See <see href="https://aka.ms/efcore-docs-entity-entries">Accessing tracked entities in EF Core</see> for more information and
  ///         examples.
  ///     </para>
  /// </remarks>
  /// <param name="entity">The entity to get the entry for.</param>
  /// <returns>The entry for the given entity.</returns>
  EntityEntry Entry(object entity);
}