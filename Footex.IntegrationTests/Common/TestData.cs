namespace Footex.IntegrationTests.Common;

public static class TestData
{
    // Sample IDs for testing - in a real implementation, these would be
    // populated with actual test data from your test database
    private static readonly Guid _sampleMatchId = Guid.Parse("f47ac10b-58cc-4372-a567-0e02b2c3d479");
    private static readonly Guid _liveMatchId = Guid.Parse("b3c56789-ab12-4d3e-9f80-7d61234e5678");
    private static readonly Guid _sampleTeamId = Guid.Parse("dea12856-c198-4129-b3f3-38250d9f2152");
    private static readonly Guid _samplePlayerId = Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7");
    private static readonly Guid _sampleSeasonId = Guid.Parse("e0a8d3d0-770a-4f89-a9c0-7e6d4e1b4c0e");
    private static readonly Guid _sampleStadiumId = Guid.Parse("b23a3d3c-a434-4ba5-9f7a-059d041f55b3");
    private static readonly Guid _sampleCoachId = Guid.Parse("afc65e75-d452-4a17-94f6-c3abcd92a4b1");

    public static Guid GetSampleMatchId() => _sampleMatchId;
    public static Guid GetLiveMatchId() => _liveMatchId;
    public static Guid GetSampleTeamId() => _sampleTeamId;
    public static Guid GetSamplePlayerId() => _samplePlayerId;
    public static Guid GetSampleSeasonId() => _sampleSeasonId;
    public static Guid GetSampleStadiumId() => _sampleStadiumId;
    public static Guid GetSampleCoachId() => _sampleCoachId;
}
