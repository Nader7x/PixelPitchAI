using Domain.Models;
using Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services;

public class MatchEventBroadcasterTests
{
    [Fact]
    public async Task DisposingDuringReplayRemovesSubscriber()
    {
        var broadcaster = new MatchEventBroadcaster(NullLogger<MatchEventBroadcaster>.Instance);
        await broadcaster.BroadcastEventAsync("42", new FootballMatchEvent { match_id = "42", event_index = 1, action = "match_start" });
        var subscription = broadcaster.SubscribeAsync("42", default).GetAsyncEnumerator();
        Assert.True(await subscription.MoveNextAsync());
        await subscription.DisposeAsync();
        var field = typeof(MatchEventBroadcaster).GetField("_subscribers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var subscribers = field.GetValue(broadcaster)!;
        Assert.Equal(0, (int)subscribers.GetType().GetProperty("Count")!.GetValue(subscribers)!);
    }

    [Fact]
    public async Task ReplayThenLivePreservesOrderWithoutDuplicates()
    {
        var broadcaster = new MatchEventBroadcaster(NullLogger<MatchEventBroadcaster>.Instance);
        await broadcaster.BroadcastEventAsync("42", new FootballMatchEvent { match_id = "42", event_index = 1, action = "pass" });
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await using var subscription = broadcaster.SubscribeAsync("42", timeout.Token).GetAsyncEnumerator();
        Assert.True(await subscription.MoveNextAsync());
        Assert.Equal(1, subscription.Current.EventIndex);
        await broadcaster.BroadcastEventAsync("42", new FootballMatchEvent { match_id = "42", event_index = 2, action = "pass" });
        Assert.True(await subscription.MoveNextAsync());
        Assert.Equal(2, subscription.Current.EventIndex);
    }
}
