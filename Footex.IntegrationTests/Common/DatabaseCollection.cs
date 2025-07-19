using Xunit;

namespace Footex.IntegrationTests.Common;

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<FootexWebApplicationFactory> { }
