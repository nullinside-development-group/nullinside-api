namespace Nullinside.Api.Common;

/// <summary>
///   The API-defined roles the user can have to access various parts of the site.
/// </summary>
public enum UserRoles {
  /// <summary>
  ///   The role of a currently authenticated user. This role has no other access. It is considered "public" access.
  /// </summary>
  USER,

  /// <summary>
  ///   The role of the administrator that has access to exclusive and highly sensitive functions.
  /// </summary>
  ADMIN,

  /// <summary>
  ///   The roles of a virtual machine administrator that provides access to the docker images hosted by the site.
  /// </summary>
  VM_ADMIN
}