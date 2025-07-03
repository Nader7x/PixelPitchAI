using NBomber.Http;

// Corrected: Using System.Net.Http.HttpResponseMessage

// NBomber's HTTP response type

namespace Footex.PerformanceTests.Common;

public class PerformanceTestHelpers
{
    // Corrected generic type to HttpResponseMessage
    public static void AssertResponseSuccess(
        HttpResponse<HttpResponseMessage> response,
        string testName
    )
    {
        // Accessing IsSuccessStatusCode directly from the inner HttpResponseMessage
        if (!response.Response.IsSuccessStatusCode)
            // Accessing StatusCode from the inner HttpResponseMessage
            throw new Exception(
                $"{testName} failed with status code: {response.Response.StatusCode}"
            );
    }

    // Corrected: Now takes elapsedMs as a parameter, as NBomber.Http.HttpResponse<T> doesn't expose it directly
    // when using WebApplicationFactory's client. You'll measure this with Stopwatch in your step.
    public static void AssertResponseTime(long elapsedMs, TimeSpan maxResponseTime, string testName)
    {
        if (elapsedMs > maxResponseTime.TotalMilliseconds && maxResponseTime.TotalMilliseconds != 0) // Added check for 0 maxResponseTime
            throw new Exception(
                $"{testName} exceeded max response time: {elapsedMs}ms > {maxResponseTime.TotalMilliseconds}ms"
            );
    }

    // Corrected generic type to HttpResponseMessage
    public static void AssertCacheHeader(
        HttpResponse<HttpResponseMessage> response,
        bool expectedCacheHit,
        string testName
    )
    {
        // Accessing Headers from the inner HttpResponseMessage
        var hasCacheHeader = response.Response.Headers.TryGetValues(
            "X-Cache-Hit",
            out var cacheHeaderValue
        );

        if (!hasCacheHeader)
            throw new Exception($"{testName} missing cache header");

        var actualCacheHit = bool.Parse(cacheHeaderValue?.FirstOrDefault() ?? string.Empty); // Use FirstOrDefault for enumerable headers
        if (actualCacheHit != expectedCacheHit)
            throw new Exception(
                $"{testName} cache hit mismatch: expected {expectedCacheHit}, got {actualCacheHit}"
            );
    }

    public static string[] GetRealisticSearchQueries()
    {
        return
        [
            // Player names
            "messi",
            "ronaldo",
            "neymar",
            "mbappe",
            "haaland",
            "salah",
            "de bruyne",
            "lewandowski",
            "benzema",
            "modric",
            "kane",
            "son",
            "rashford",
            // Team names
            "manchester united",
            "liverpool",
            "barcelona",
            "real madrid",
            "bayern munich",
            "juventus",
            "psg",
            "arsenal",
            "chelsea",
            "tottenham",
            "city",
            "milan",
            // Countries/Nationalities
            "brazil",
            "argentina",
            "france",
            "germany",
            "spain",
            "england",
            "portugal",
            "netherlands",
            "italy",
            "belgium",
            // Positions
            "midfielder",
            "striker",
            "defender",
            "goalkeeper",
            "winger",
            "centre-back",
            // Combinations
            "brazilian striker",
            "english midfielder",
            "spanish defender",
            "manchester player",
            "liverpool forward",
            "barcelona midfielder",
        ];
    }

    public static int[] GetRealisticPlayerIds()
    {
        // Simulate realistic player ID ranges
        return Enumerable.Range(1, 500).ToArray();
    }

    public static int[] GetRealisticTeamIds()
    {
        // Simulate realistic team ID ranges
        return Enumerable.Range(1, 50).ToArray();
    }

    public static string[] GetRealisticNationalities()
    {
        return new[]
        {
            "Brazil",
            "Argentina",
            "France",
            "Germany",
            "Spain",
            "England",
            "Portugal",
            "Netherlands",
            "Italy",
            "Belgium",
            "Croatia",
            "Poland",
        };
    }

    public static string[] GetRealisticCountries()
    {
        return new[]
        {
            "England",
            "Spain",
            "Italy",
            "Germany",
            "France",
            "Brazil",
            "Argentina",
            "Netherlands",
            "Portugal",
            "Belgium",
        };
    }

    public static (int pageNumber, int pageSize)[] GetRealisticPaginationParams()
    {
        return new[] { (1, 10), (1, 20), (1, 50), (2, 10), (2, 20), (3, 10), (5, 10) };
    }
}
