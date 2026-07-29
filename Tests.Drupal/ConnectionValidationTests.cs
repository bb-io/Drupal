using Apps.Drupal.Connections;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Tests.Drupal.Base;

namespace Tests.Drupal;

[TestClass]
public class ConnectionValidationTests : TestBase
{
    [TestMethod]
    [DrupalVersionDataSource]
    public async Task ValidateConnection_ValidFixtureKey_ReturnsValid(int version)
    {
        // Arrange
        var context = StartFixture(version);
        var validator = new ConnectionValidator();

        // Act
        var result = await validator.ValidateConnection(
            context.AuthenticationCredentialsProviders,
            CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    [DrupalVersionDataSource]
    public async Task ValidateConnection_InvalidFixtureKey_ThrowsApplicationException(int version)
    {
        // Arrange
        _ = StartFixture(version);
        var context = CreateInvocationContext(FixtureServer.Url, FixtureApiServer.InvalidApiKey);
        var validator = new ConnectionValidator();

        // Act
        var exception = await Assert.ThrowsExactlyAsync<PluginApplicationException>(async () =>
            await validator.ValidateConnection(
                context.AuthenticationCredentialsProviders,
                CancellationToken.None));

        // Assert
        StringAssert.Contains(exception.Message, "403");
    }
}
