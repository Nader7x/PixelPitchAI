using Domain.Models;
using FluentAssertions;
using Infrastructure.Services;
using Xunit;

namespace Footex.UnitTests.Services;

public class ZeroAllocationEventParserTests
{
    [Fact]
    public void TryParseEvent_ValidPassEvent_ParsesAllFieldsCorrectly()
    {
        // Arrange
        var line = "00:00 -  Atlético_Madrid_2019  - pass by Antoine Griezmann at (60.0, 40.0), type: Kick Off, height: Ground Pass, to (50.0, 36.0), outcome: Complete";
        var homeScore = 0;
        var awayScore = 0;

        // Act
        var success = ZeroAllocationEventParser.TryParseEvent(
            line.AsSpan(),
            matchId: "42",
            eventIndex: 1,
            ref homeScore,
            ref awayScore,
            homeTeamName: "Atlético_Madrid_2019",
            awayTeamName: "Barcelona_2017",
            out var matchEvent
        );

        // Assert
        success.Should().BeTrue();
        matchEvent.Should().NotBeNull();
        matchEvent!.timestamp.Should().Be("00:00");
        matchEvent.time_seconds.Should().Be(0);
        matchEvent.minute.Should().Be(0);
        matchEvent.second.Should().Be(0);
        matchEvent.team.Should().Be("Atlético_Madrid_2019");
        matchEvent.player.Should().Be("Antoine Griezmann");
        matchEvent.action.Should().Be("pass");
        matchEvent.type.Should().Be("Kick Off");
        matchEvent.height.Should().Be("Ground Pass");
        matchEvent.outcome.Should().Be("Complete");
        matchEvent.position.Should().BeEquivalentTo(new[] { 60.0f, 40.0f });
        matchEvent.pass_target.Should().BeEquivalentTo(new[] { 50.0f, 36.0f });
        matchEvent.event_index.Should().Be(1);
        matchEvent.match_id.Should().Be("42");
    }

    [Fact]
    public void TryParseEvent_GoalEvent_UpdatesScoreCorrectly()
    {
        // Arrange
        var line = "14:23 -  Barcelona_2017  - shot by Lionel Messi at (85.0, 38.0), aimed at (95.0, 40.0), outcome: Goal";
        var homeScore = 0;
        var awayScore = 0;

        // Act
        var success = ZeroAllocationEventParser.TryParseEvent(
            line.AsSpan(),
            matchId: "42",
            eventIndex: 5,
            ref homeScore,
            ref awayScore,
            homeTeamName: "Atlético_Madrid_2019",
            awayTeamName: "Barcelona_2017",
            out var matchEvent
        );

        // Assert
        success.Should().BeTrue();
        matchEvent.Should().NotBeNull();
        matchEvent!.outcome.Should().Be("Goal");
        matchEvent.player.Should().Be("Lionel Messi");
        awayScore.Should().Be(1);
        homeScore.Should().Be(0);
        matchEvent.Score.Should().NotBeNull();
        matchEvent.Score!.Away.Should().Be(1);
        matchEvent.Score.Home.Should().Be(0);
    }

    [Fact]
    public void TryParseEvent_OwnGoal_CreditsOpposingTeamScore()
    {
        // Arrange: Home team scores an own goal -> away team score must increment
        var line = "28:15 -  Atlético_Madrid_2019  - own goal by Stefan Savic at (15.0, 38.0), outcome: Goal";
        var homeScore = 0;
        var awayScore = 0;

        // Act
        var success = ZeroAllocationEventParser.TryParseEvent(
            line.AsSpan(),
            matchId: "42",
            eventIndex: 6,
            ref homeScore,
            ref awayScore,
            homeTeamName: "Atlético_Madrid_2019",
            awayTeamName: "Barcelona_2017",
            out var matchEvent
        );

        // Assert
        success.Should().BeTrue();
        matchEvent.Should().NotBeNull();
        homeScore.Should().Be(0);
        awayScore.Should().Be(1);
        matchEvent!.Score.Should().NotBeNull();
        matchEvent.Score!.Home.Should().Be(0);
        matchEvent.Score.Away.Should().Be(1);
    }

    [Theory]
    [InlineData("[MATCH START]", "match_start", 0)]
    [InlineData("[END OF FIRST HALF]", "first_half_end", 2700)]
    [InlineData("[SECOND HALF START]", "second_half_start", 2700)]
    [InlineData("[MATCH END]", "match_end", 5400)]
    public void TryParseEvent_SystemMarkers_CreatesSystemEvents(string marker, string expectedAction, int expectedSeconds)
    {
        // Arrange
        var homeScore = 1;
        var awayScore = 2;

        // Act
        var success = ZeroAllocationEventParser.TryParseEvent(
            marker.AsSpan(),
            matchId: "100",
            eventIndex: 10,
            ref homeScore,
            ref awayScore,
            homeTeamName: "TeamA",
            awayTeamName: "TeamB",
            out var matchEvent
        );

        // Assert
        success.Should().BeTrue();
        matchEvent.Should().NotBeNull();
        matchEvent!.team.Should().Be("SYSTEM");
        matchEvent.action.Should().Be(expectedAction);
        matchEvent.time_seconds.Should().Be(expectedSeconds);
        matchEvent.Score!.Home.Should().Be(1);
        matchEvent.Score.Away.Should().Be(2);
    }

    [Fact]
    public void TryParseEvent_HeaderOrIgnoredMarkers_ReturnsFalse()
    {
        var homeScore = 0;
        var awayScore = 0;

        var success = ZeroAllocationEventParser.TryParseEvent(
            "[EVENTS START]".AsSpan(),
            matchId: "100",
            eventIndex: 1,
            ref homeScore,
            ref awayScore,
            "TeamA",
            "TeamB",
            out var matchEvent
        );

        success.Should().BeFalse();
        matchEvent.Should().BeNull();
    }
}
