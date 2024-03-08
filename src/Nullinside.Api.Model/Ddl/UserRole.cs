using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Common;

namespace Nullinside.Api.Model.Ddl;

/// <summary>
///   A role a user can have in the application. This provides the user with access to different parts of the site.
/// </summary>
public class UserRole : ITableModel {
  /// <summary>
  ///   The unique identifier of the role.
  /// </summary>
  public int Id { get; set; }

  /// <summary>
  ///   The <seealso cref="User.Id" /> associated with this role.
  /// </summary>
  public int UserId { get; set; }

  /// <summary>
  ///   The API-defined role this user has.
  /// </summary>
  public UserRoles Role { get; set; }

  /// <summary>
  ///   The timestamp of when the user received this role.
  /// </summary>
  public DateTime RoleAdded { get; set; }

  /// <summary>
  ///   The method used to configure the POCOs of the table.
  /// </summary>
  /// <param name="modelBuilder">The model builder.</param>
  public void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.Entity<UserRole>(entity => {
      entity.HasKey(e => e.Id);
    });
  }
}