using Nullinside.Api.Model.Ddl;
using Nullinside.Api.Model.Shared;

namespace Nullinside.Api.Tests.Nullinside.Api.Model.Shared;

/// <summary>
///   Tests for the <see cref="UserHelpers" /> class.
/// </summary>
public class UserHelpersTests : UnitTestBase {
  /// <summary>
  ///   The case where a user is generating a new token to replace their existing one.
  /// </summary>
  [Test]
  public async Task GenerateTokenForExistingUser() {
    _db.Users.Add(
      new User {
        Email = "email"
      }
    );
    await _db.SaveChangesAsync().ConfigureAwait(false);

    // Verify there is only one user
    Assert.That(_db.Users.Count(), Is.EqualTo(1));

    // Generate a new token
    string? token = await UserHelpers.GenerateTokenAndSaveToDatabase(_db, "email").ConfigureAwait(false);
    Assert.That(token, Is.Not.Null);

    // Verify we still only have one user
    Assert.That(_db.Users.Count(), Is.EqualTo(1));
    Assert.That(_db.Users.First().Token, Is.EqualTo(token));
  }

  /// <summary>
  ///   The case where a user is getting a token for the first time. A new user should be created. The existing user
  ///   should be untouched.
  /// </summary>
  [Test]
  public async Task GenerateTokenForNewUser() {
    _db.Users.Add(
      new User {
        Email = "email2"
      }
    );
    await _db.SaveChangesAsync().ConfigureAwait(false);

    // Verify there is only one user
    Assert.That(_db.Users.Count(), Is.EqualTo(1));

    // Generate a new token
    string? token = await UserHelpers.GenerateTokenAndSaveToDatabase(_db, "email").ConfigureAwait(false);
    Assert.That(token, Is.Not.Null);

    // Verify we have a new user
    Assert.That(_db.Users.Count(), Is.EqualTo(2));
    Assert.That(_db.Users.FirstOrDefault(u => u.Email == "email")?.Token, Is.EqualTo(token));

    // Verfy the old user is untouched
    Assert.That(_db.Users.FirstOrDefault(u => u.Email == "email2")?.Token, Is.Null);
  }

  /// <summary>
  ///   Unexpected database errors should result in a null being returned.
  /// </summary>
  [Test]
  public async Task HandleUnexpectedErrors() {
    // Force an error to occur.
    string? token = await UserHelpers.GenerateTokenAndSaveToDatabase(null!, "email").ConfigureAwait(false);
    Assert.That(token, Is.Null);
  }
}