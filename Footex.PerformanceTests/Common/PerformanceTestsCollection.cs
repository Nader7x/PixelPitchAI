using Footex.IntegrationTests.Common;
using Xunit;

namespace Footex.PerformanceTests.Common;

[CollectionDefinition("Performance tests collection")]
public class PerformanceTestsCollection : ICollectionFixture<FootexWebApplicationFactory> { }
