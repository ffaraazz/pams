namespace PAMS.IntegrationTests.Infrastructure;

/// <summary>
/// xUnit collection definition for sharing the PamsApiFactory across test classes.
/// All endpoint test classes should use [Collection("PamsApi")] to share the same container.
/// </summary>
[CollectionDefinition("PamsApi")]
public sealed class PamsApiCollection : ICollectionFixture<PamsApiFactory>
{
}
