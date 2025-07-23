using Microsoft.AspNetCore.Mvc;

using Nullinside.Api.Controllers;
using Nullinside.Api.Model.Ddl;

namespace Nullinside.Api.Tests.Nullinside.Api.Controllers;

/// <summary>
///   Tests for the <see cref="FeatureToggleController" /> class
/// </summary>
public class FeatureToggleControllerTests : UnitTestBase {
  /// <summary>
  ///   Tests that we can pull the feature toggles. It's basically a straight through.
  /// </summary>
  [Test]
  public async Task GetAllFeatureToggles() {
    // Creates two feature toggles.
    _db.FeatureToggle.AddRange(
      new FeatureToggle {
        Id = 1, Feature = "hi", IsEnabled = true
      },
      new FeatureToggle {
        Id = 2, Feature = "bye", IsEnabled = false
      }
    );
    await _db.SaveChangesAsync().ConfigureAwait(false);

    // Make the call and ensure it's successful.
    var controller = new FeatureToggleController(_db);
    ObjectResult obj = await controller.GetAll().ConfigureAwait(false);
    Assert.That(obj.StatusCode, Is.EqualTo(200));

    // Ensure they passed through cleanly.
    var featureToggles = obj.Value as IEnumerable<FeatureToggle>;
    Assert.That(featureToggles, Is.Not.Null);
    Assert.That(featureToggles.Count, Is.EqualTo(2));
    Assert.That(featureToggles.FirstOrDefault(f => f.Feature == "hi")?.IsEnabled, Is.True);
    Assert.That(featureToggles.FirstOrDefault(f => f.Feature == "bye")?.IsEnabled, Is.False);
  }
}