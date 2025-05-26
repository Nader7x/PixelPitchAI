using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Repositories;


public class MatchRepository(FootballDbContext context) : Repository<Match>(context), IMatchRepository
{
    private readonly FootballDbContext _context = context;


    public async Task<IReadOnlyList<Match>> GetBySeasonIdAsync(int homeSeasonId, int awaySeasonId)
    {
        return await _context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.HomeTeamSeason)
            .Include(m => m.AwayTeamSeason)
            .Where(m => m.HomeTeamSeasonId == homeSeasonId && m.AwayTeamSeasonId == awaySeasonId)
            .OrderByDescending(m => m.ScheduledDateTimeUtc)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Match>> GetByTeamIdAsync(int teamId)
    {
        return await _context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.HomeTeamSeason)
            .Where(m => m.HomeTeamId == teamId || m.AwayTeamId == teamId)
            .OrderByDescending(m => m.ScheduledDateTimeUtc)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Match>> GetByDateRangeAsync(DateTime start, DateTime end)
    {
        return await _context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.HomeTeamSeason)
            .Include(m => m.AwayTeamSeason)
            .Where(m => m.ScheduledDateTimeUtc >= start && m.ScheduledDateTimeUtc <= end)
            .OrderBy(m => m.ScheduledDateTimeUtc)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Match>> GetUpcomingMatchesAsync(int count)
    {
        var now = DateTime.UtcNow;
        return await _context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.HomeTeamSeason)
            .Include(m => m.AwayTeamSeason)
            .Where(m => m.ScheduledDateTimeUtc > now)
            .OrderBy(m => m.ScheduledDateTimeUtc)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Match>> GetRecentMatchesAsync(int count)
    {
        var now = DateTime.UtcNow;
        return await _context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.HomeTeamSeason)
            .Include(m => m.AwayTeamSeason)
            .Where(m => m.ScheduledDateTimeUtc <= now && m.MatchStatus == "Completed")
            .OrderByDescending(m => m.ScheduledDateTimeUtc)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Match>> GetByStatusAsync(string status)
    {
        return await _context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.HomeTeamSeason)
            .Include(m => m.AwayTeamSeason)
            .Where(m => m.MatchStatus.ToLower() == status.ToLower())
            .OrderByDescending(m => m.ScheduledDateTimeUtc)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Match>> GetAllWithDetailsAsync()
    {
        return await _context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.HomeTeamSeason)
            .Include(m => m.AwayTeamSeason)
            .Include(m => m.Stadium)
            .ToListAsync();
    }

    public async Task<Match?> GetByIdWithDetailsAsync(int matchId)
    {
        return await _context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.HomeTeamSeason)
            .Include(m => m.AwayTeamSeason)
            .Include(m => m.Stadium)
            .Include(m => m.Creator)
            .Include(m => m.MatchEvents)
            .FirstOrDefaultAsync(m => m != null && m.Id == matchId);
    }

    public async Task<IEnumerable<Match>> SearchAsync(string query)
    {
        return await _context.Matches
            .Where(m =>
                m.HomeTeam.Name.ToLower().Contains(query) ||
                m.AwayTeam.Name.ToLower().Contains(query))
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Match>> GetMatchesBySeasonIdAsync(int seasonId)
    {
        return await _context.Matches
            .Where(m => m.HomeTeamSeasonId == seasonId || m.AwayTeamSeasonId == seasonId).ToListAsync();
    }

    public async Task<int> GetLiveMatchAsync(string requestUserId)
    {
        return await _context.Matches
            .Where(m => m.MatchStatus == "Live" && m.CreatorId == requestUserId)
            .Select(m => m.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<Match?> UpdateSimulationIdAsync(int matchId, string simulationId , CancellationToken cancellationToken)
    {
        var match = await _context.Matches.FindAsync([matchId, cancellationToken], cancellationToken: cancellationToken);
        if (match == null)
        {
            return null;
        }
        match.SimulationId = simulationId;
        _context.Matches.Update(match);
        await _context.SaveChangesAsync(cancellationToken);
        
        return match;
    }
}