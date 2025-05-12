using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class MatchRepository(FootballDbContext context) : IMatchRepository
{
    public async Task<Match?> GetByIdAsync(int id)
    {
        return await context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Season)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IReadOnlyList<Match>> GetAllAsync()
    {
        return await context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Season)
            .OrderByDescending(m => m.ScheduledDateTimeUTC)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Match>> GetBySeasonIdAsync(int seasonId)
    {
        return await context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Where(m => m.SeasonId == seasonId)
            .OrderByDescending(m => m.ScheduledDateTimeUTC)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Match>> GetByTeamIdAsync(int teamId)
    {
        return await context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Season)
            .Where(m => m.HomeTeamId == teamId || m.AwayTeamId == teamId)
            .OrderByDescending(m => m.ScheduledDateTimeUTC)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Match>> GetByDateRangeAsync(DateTime start, DateTime end)
    {
        return await context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Season)
            .Where(m => m.ScheduledDateTimeUTC >= start && m.ScheduledDateTimeUTC <= end)
            .OrderBy(m => m.ScheduledDateTimeUTC)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Match>> GetUpcomingMatchesAsync(int count)
    {
        var now = DateTime.UtcNow;
        return await context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Season)
            .Where(m => m.ScheduledDateTimeUTC > now)
            .OrderBy(m => m.ScheduledDateTimeUTC)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Match>> GetRecentMatchesAsync(int count)
    {
        var now = DateTime.UtcNow;
        return await context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Season)
            .Where(m => m.ScheduledDateTimeUTC <= now && m.MatchStatus == "Completed")
            .OrderByDescending(m => m.ScheduledDateTimeUTC)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Match>> GetByStatusAsync(string status)
    {
        return await context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Season)
            .Where(m => m.MatchStatus.ToLower() == status.ToLower())
            .OrderByDescending(m => m.ScheduledDateTimeUTC)
            .ToListAsync();
    }

    public async Task<Match> AddAsync(Match match)
    {
        await context.Matches.AddAsync(match);
        return match;
    }

    public void Update(Match match)
    {
        context.Matches.Attach(match);
        context.Entry(match).State = EntityState.Modified;
    }

    public void Remove(Match match)
    {
        context.Matches.Remove(match);
    }
}