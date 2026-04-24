using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Nullinside.Api.Common.AspNetCore.Middleware;
using Nullinside.Api.Model;
using NUnit.Framework;

namespace Nullinside.Api.Tests.Nullinside.Api.Common.AspNetCore.Middleware;

[TestFixture]
public class BasicAuthenticationHandlerTests : UnitTestBase {
  private Mock<IOptionsMonitor<AuthenticationSchemeOptions>> _options;
  private Mock<ILoggerFactory> _loggerFactory;
  private Mock<UrlEncoder> _encoder;

  [SetUp]
  public override void Setup() {
    base.Setup();
    _options = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
    _options.Setup(x => x.Get(It.IsAny<string>())).Returns(new AuthenticationSchemeOptions());
    _loggerFactory = new Mock<ILoggerFactory>();
    _loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
    _encoder = new Mock<UrlEncoder>();
  }

  [Test]
  public async Task HandleAuthenticateAsync_ReturnsNoResult_WhenMethodIsOptions() {
    // Arrange
    var handler = new BasicAuthenticationHandler(_options.Object, _loggerFactory.Object, _encoder.Object, _db);
    var context = new DefaultHttpContext();
    context.Request.Method = HttpMethods.Options;
    
    var scheme = new AuthenticationScheme("Bearer", "Bearer", typeof(BasicAuthenticationHandler));
    await handler.InitializeAsync(scheme, context).ConfigureAwait(false);

    // Act
    var result = await handler.AuthenticateAsync().ConfigureAwait(false);

    // Assert
    Assert.That(result.None, Is.True, "OPTIONS request should return NoResult (result.None should be true)");
  }

  [Test]
  public async Task HandleAuthenticateAsync_ReturnsFail_WhenNoTokenProvided() {
    // Arrange
    var handler = new BasicAuthenticationHandler(_options.Object, _loggerFactory.Object, _encoder.Object, _db);
    var context = new DefaultHttpContext();
    context.Request.Method = HttpMethods.Get;
    
    var scheme = new AuthenticationScheme("Bearer", "Bearer", typeof(BasicAuthenticationHandler));
    await handler.InitializeAsync(scheme, context).ConfigureAwait(false);

    // Act
    var result = await handler.AuthenticateAsync().ConfigureAwait(false);

    // Assert
    Assert.That(result.Succeeded, Is.False);
    Assert.That(result.Failure?.Message, Is.EqualTo("No token"));
  }
}
