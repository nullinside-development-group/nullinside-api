using System.Security.Claims;

using Google.Apis.Auth;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;

using Moq;

using Nullinside.Api.Common.Twitch;
using Nullinside.Api.Controllers;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;
using Nullinside.Api.Shared;
using Nullinside.Api.Shared.Json;

namespace Nullinside.Api.Tests.Nullinside.Api.Controllers;

/// <summary>
///   Tests for the <see cref="FeatureToggleController" /> class
/// </summary>
public class UserControllerTests : UnitTestBase {
  /// <summary>
  ///   The mock configuration.
  /// </summary>
  private IConfiguration _configuration;

  /// <summary>
  ///   The twitch api.
  /// </summary>
  private Mock<ITwitchApiProxy> _twitchApi;

  /// <summary>
  ///   The web socket persister.
  /// </summary>
  private Mock<IWebSocketPersister> _webSocketPersister;

  /// <inheritdoc />
  public override void Setup() {
    base.Setup();

    // Setup the config
    var config = new Dictionary<string, string?> {
      { "Api:SiteUrl", string.Empty }
    };

    _configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(config)
      .Build();

    _twitchApi = new Mock<ITwitchApiProxy>();
    _webSocketPersister = new Mock<IWebSocketPersister>();
  }

  /// <summary>
  ///   Tests that we can login with google for a user that already exists.
  /// </summary>
  [Test]
  public async Task PerformGoogleLoginExisting() {
    // Create an existing user.
    _db.Users.Add(new User {
      Id = 1,
      Email = "hi"
    });

    await _db.SaveChangesAsync().ConfigureAwait(false);

    // Make the call and ensure it's successful.
    var controller = new TestableUserController(_configuration, _db, _webSocketPersister.Object);
    controller.Email = "hi";
    RedirectResult obj = await controller.Login(new GoogleOpenIdToken { credential = "stuff" }).ConfigureAwait(false);

    // We should have been redirected to the successful route.
    Assert.That(obj.Url.StartsWith("/user/login?token="), Is.True);

    // No additional users should have been created.
    Assert.That(_db.Users.Count(), Is.EqualTo(1));

    // We should have saved the token in the existing user's database. 
    Assert.That(obj.Url.EndsWith(_db.Users.First().Token!), Is.True);
  }

  /// <summary>
  ///   Tests that we can login with google for a new user.
  /// </summary>
  [Test]
  public async Task PerformGoogleLoginNewUser() {
    // Make the call and ensure it's successful.
    var controller = new TestableUserController(_configuration, _db, _webSocketPersister.Object);
    controller.Email = "hi";
    RedirectResult obj = await controller.Login(new GoogleOpenIdToken { credential = "stuff" }).ConfigureAwait(false);

    // We should have been redirected to the successful route.
    Assert.That(obj.Url.StartsWith("/user/login?token="), Is.True);

    // No additional users should have been created.
    Assert.That(_db.Users.Count(), Is.EqualTo(1));

    // We should have saved the token in the existing user's database. 
    Assert.That(obj.Url.EndsWith(_db.Users.First().Token!), Is.True);
  }

  /// <summary>
  ///   Tests that we handle DB errors correctly in the google login.
  /// </summary>
  [Test]
  public async Task GoToErrorOnDbException() {
    _db.Users = null!;

    // Make the call and ensure it's successful.
    var controller = new TestableUserController(_configuration, _db, _webSocketPersister.Object);
    controller.Email = "hi";
    RedirectResult obj = await controller.Login(new GoogleOpenIdToken { credential = "stuff" }).ConfigureAwait(false);

    // We should have been redirected to the error.
    Assert.That(obj.Url.StartsWith("/user/login?error="), Is.True);
  }

  /// <summary>
  ///   Tests that we handle bad gmail responses.
  /// </summary>
  [Test]
  public async Task GoToErrorOnBadGmailResponse() {
    // Make the call and ensure it's successful.
    var controller = new TestableUserController(_configuration, _db, _webSocketPersister.Object);
    controller.Email = null;
    RedirectResult obj = await controller.Login(new GoogleOpenIdToken { credential = "stuff" }).ConfigureAwait(false);

    // We should have been redirected to the error.
    Assert.That(obj.Url.StartsWith("/user/login?error="), Is.True);
  }

  /// <summary>
  ///   Tests that we can login with twitch for a user that already exists.
  /// </summary>
  [Test]
  public async Task PerformTwitchLoginExisting() {
    // Tells us twitch parsed the code successfully.
    _twitchApi.Setup(a => a.CreateAccessToken(It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .Returns(() => Task.FromResult<TwitchAccessToken?>(new TwitchAccessToken()));

    // Gets a matching email address from our database
    _twitchApi.Setup(a => a.GetUserEmail(It.IsAny<CancellationToken>()))
      .Returns(() => Task.FromResult<string?>("hi"));

    // Create an existing user.
    _db.Users.Add(new User {
      Id = 1,
      Email = "hi"
    });

    await _db.SaveChangesAsync().ConfigureAwait(false);

    // Make the call and ensure it's successful.
    var controller = new TestableUserController(_configuration, _db, _webSocketPersister.Object);
    RedirectResult obj = await controller.TwitchLogin("things", _twitchApi.Object).ConfigureAwait(false);

    // We should have been redirected to the successful route.
    Assert.That(obj.Url.StartsWith("/user/login?token="), Is.True);

    // No additional users should have been created.
    Assert.That(_db.Users.Count(), Is.EqualTo(1));

    // We should have saved the token in the existing user's database. 
    Assert.That(obj.Url.EndsWith(_db.Users.First().Token!), Is.True);
  }

  /// <summary>
  ///   Tests that we can login with twitch for a new user.
  /// </summary>
  [Test]
  public async Task PerformTwitchLoginNewUser() {
    // Tells us twitch parsed the code successfully.
    _twitchApi.Setup(a => a.CreateAccessToken(It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .Returns(() => Task.FromResult<TwitchAccessToken?>(new TwitchAccessToken()));

    // Gets a matching email address from our database
    _twitchApi.Setup(a => a.GetUserEmail(It.IsAny<CancellationToken>()))
      .Returns(() => Task.FromResult<string?>("hi"));

    // Make the call and ensure it's successful.
    var controller = new TestableUserController(_configuration, _db, _webSocketPersister.Object);
    RedirectResult obj = await controller.TwitchLogin("things", _twitchApi.Object).ConfigureAwait(false);

    // We should have been redirected to the successful route.
    Assert.That(obj.Url.StartsWith("/user/login?token="), Is.True);

    // No additional users should have been created.
    Assert.That(_db.Users.Count(), Is.EqualTo(1));

    // We should have saved the token in the existing user's database. 
    Assert.That(obj.Url.EndsWith(_db.Users.First().Token!), Is.True);
  }

  /// <summary>
  ///   Tests that we handle a bad response from twitch.
  /// </summary>
  [Test]
  public async Task PerformTwitchLoginBadTwitchResponse() {
    // Tells us twitch thinks it was a bad code.
    _twitchApi.Setup(a => a.CreateAccessToken(It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .Returns(() => Task.FromResult<TwitchAccessToken?>(null));

    // Make the call and ensure it's successful.
    var controller = new TestableUserController(_configuration, _db, _webSocketPersister.Object);
    RedirectResult obj = await controller.TwitchLogin("things", _twitchApi.Object).ConfigureAwait(false);

    // We should have gone down the bad route
    Assert.That(obj.Url.StartsWith("/user/login?error="), Is.True);
  }

  /// <summary>
  ///   Tests that we can login with twitch but it has no email associated with the account.
  /// </summary>
  [Test]
  public async Task PerformTwitchLoginWithNoEmailAccount() {
    // Tells us twitch parsed the code successfully.
    _twitchApi.Setup(a => a.CreateAccessToken(It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .Returns(() => Task.FromResult<TwitchAccessToken?>(new TwitchAccessToken()));

    // Make the call and ensure it's successful.
    var controller = new TestableUserController(_configuration, _db, _webSocketPersister.Object);
    RedirectResult obj = await controller.TwitchLogin("things", _twitchApi.Object).ConfigureAwait(false);

    // We should have gone down the bad route because no email was associated with the twitch account.
    Assert.That(obj.Url.StartsWith("/user/login?error="), Is.True);
  }

  /// <summary>
  ///   Tests that we handle database failures correctly in the twitch login process.
  /// </summary>
  [Test]
  public async Task PerformTwitchLoginDbFailure() {
    _db.Users = null!;

    // Tells us twitch parsed the code successfully.
    _twitchApi.Setup(a => a.CreateAccessToken(It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .Returns(() => Task.FromResult<TwitchAccessToken?>(new TwitchAccessToken()));

    // Gets an email address from twitch
    _twitchApi.Setup(a => a.GetUserEmail(It.IsAny<CancellationToken>()))
      .Returns(() => Task.FromResult<string?>("hi"));

    // Make the call and ensure it's successful.
    var controller = new TestableUserController(_configuration, _db, _webSocketPersister.Object);
    RedirectResult obj = await controller.TwitchLogin("things", _twitchApi.Object).ConfigureAwait(false);

    // We should have been redirected to the error route because of an exception in DB processing.
    Assert.That(obj.Url.StartsWith("/user/login?error="), Is.True);
  }

  /// <summary>
  ///   Tests that we get the roles assigned to the user.
  /// </summary>
  [Test]
  public void GetRoles() {
    // Setup the logged in user
    var claims = new List<Claim> {
      new(ClaimTypes.Role, "candy")
    };
    var identity = new ClaimsIdentity(claims, "icecream");

    // Make the call and ensure it's successful.
    var controller = new TestableUserController(_configuration, _db, _webSocketPersister.Object);
    controller.ControllerContext = new ControllerContext();
    controller.ControllerContext.HttpContext = new DefaultHttpContext();
    controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

    ObjectResult obj = controller.GetRoles();
    Assert.That(obj.StatusCode, Is.EqualTo(200));

    // Ensure we got the role we put in.
    var roles = obj.Value!.GetType().GetProperty("roles")!.GetValue(obj.Value) as IEnumerable<string>;
    Assert.That(roles!.Count(), Is.EqualTo(1));
    Assert.That(roles!.First(), Is.EqualTo("candy"));
  }

  /// <summary>
  ///   Tests that we can validate a token that exists.
  /// </summary>
  [Test]
  public async Task ValidateTokenExists() {
    _db.Users.Add(new User { Token = "123" });
    await _db.SaveChangesAsync().ConfigureAwait(false);

    // Make the call and ensure it's successful.
    var controller = new TestableUserController(_configuration, _db, _webSocketPersister.Object);
    IActionResult obj = await controller.Validate(new AuthToken("123")).ConfigureAwait(false);
    Assert.That((obj as IStatusCodeActionResult)?.StatusCode, Is.EqualTo(200));

    // Ensure we returned that the token was correct.
    Assert.That((obj as ObjectResult)?.Value, Is.True);
  }

  /// <summary>
  ///   Tests that we do not validate tokens that do not exist.
  /// </summary>
  [Test]
  public async Task ValidateFailWithoutToken() {
    // Make the call and ensure it fails.
    var controller = new TestableUserController(_configuration, _db, _webSocketPersister.Object);
    IActionResult obj = await controller.Validate(new AuthToken("123")).ConfigureAwait(false);
    Assert.That((obj as IStatusCodeActionResult)?.StatusCode, Is.EqualTo(401));
  }

  /// <summary>
  ///   Tests that unhandled exceptions are performed appropriately.
  /// </summary>
  [Test]
  public async Task ValidateFailOnDbFailure() {
    _db.Users = null!;

    // Make the call and ensure it fails.
    var controller = new TestableUserController(_configuration, _db, _webSocketPersister.Object);
    IActionResult obj = await controller.Validate(new AuthToken("123")).ConfigureAwait(false);
    Assert.That((obj as IStatusCodeActionResult)?.StatusCode, Is.EqualTo(500));
  }
}

/// <summary>
///   A testable version of the user controller that removes 3rd party dependencies.
/// </summary>
public class TestableUserController : UserController {
  /// <inheritdoc />
  public TestableUserController(IConfiguration configuration, INullinsideContext dbContext, IWebSocketPersister webSocketPersister) : base(configuration, dbContext, webSocketPersister) {
  }

  /// <summary>
  ///   Gets or sets the email to include in the google payload.
  /// </summary>
  public string? Email { get; set; }

  /// <inheritdoc />
  protected override Task<GoogleJsonWebSignature.Payload?> GenerateUserObject(GoogleOpenIdToken creds) {
    if (null != Email) {
      return Task.FromResult<GoogleJsonWebSignature.Payload?>(new GoogleJsonWebSignature.Payload { Email = Email });
    }

    return Task.FromResult<GoogleJsonWebSignature.Payload?>(null);
  }
}