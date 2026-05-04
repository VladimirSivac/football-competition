using System;
using System.Collections.Generic;
using System.Linq;
using FootballCompetition.Data;
using FootballCompetition.Models;

namespace FootballCompetition.Services;

/// <summary>
/// Application-level read/write API. Encapsulates all of the LINQ-based
/// reporting queries described in the assignment as well as CRUD pass-throughs
/// to the underlying <see cref="FileRepository{T}"/> instances.
/// </summary>
public class FootballManagerService
{
    private readonly FileRepository<Team> _teamRepo;
    private readonly FileRepository<Player> _playerRepo;
    private readonly FileRepository<Stadium> _stadiumRepo;
    private readonly FileRepository<Match> _matchRepo;

    public FootballManagerService(
        FileRepository<Team> teamRepo,
        FileRepository<Player> playerRepo,
        FileRepository<Stadium> stadiumRepo,
        FileRepository<Match> matchRepo)
    {
        _teamRepo = teamRepo;
        _playerRepo = playerRepo;
        _stadiumRepo = stadiumRepo;
        _matchRepo = matchRepo;
    }

    // ---------------------------------------------------------------------
    //  CRUD pass-throughs (the ViewModels use these directly)
    // ---------------------------------------------------------------------

    public IReadOnlyList<Team> GetTeams() => _teamRepo.GetAll().ToList();
    public IReadOnlyList<Player> GetPlayers() => _playerRepo.GetAll().ToList();
    public IReadOnlyList<Stadium> GetStadiums() => _stadiumRepo.GetAll().ToList();
    public IReadOnlyList<Match> GetMatches() => _matchRepo.GetAll().ToList();

    public void AddTeam(Team team) => _teamRepo.Add(team);
    public void UpdateTeam(Team team) => _teamRepo.Update(team);
    public void DeleteTeam(Team team)
    {
        // Cascade: drop matches that reference this team. Players that
        // belong to the team live inside the team's own JSON document via
        // the Players list, but we keep them in the dedicated player file
        // for easy querying so we cull those too.
        var key = team.Key;
        foreach (var m in _matchRepo.GetAll().Where(m => m.Team1Key == key || m.Team2Key == key).ToList())
        {
            _matchRepo.Delete(m);
        }
        foreach (var p in team.Players.ToList())
        {
            // Only delete the player from the player file if it's actually
            // present (i.e. the team has been persisted at least once).
            var existing = _playerRepo.GetByKey(p.Key);
            if (existing != null)
            {
                _playerRepo.Delete(existing);
            }
        }
        _teamRepo.Delete(team);
    }

    public void AddPlayer(Player player, Team team)
    {
        team.Players.Add(player);
        _playerRepo.Add(player);
        _teamRepo.Update(team);
    }

    public void UpdatePlayer(Player player, Team team)
    {
        var idx = team.Players.FindIndex(p => p.Key == player.Key);
        if (idx >= 0) team.Players[idx] = player;
        _playerRepo.Update(player);
        _teamRepo.Update(team);
    }

    public void DeletePlayer(Player player, Team team)
    {
        team.Players.RemoveAll(p => p.Key == player.Key);
        _playerRepo.Delete(player);
        _teamRepo.Update(team);
    }

    public void AddStadium(Stadium stadium) => _stadiumRepo.Add(stadium);
    public void UpdateStadium(Stadium stadium) => _stadiumRepo.Update(stadium);
    public void DeleteStadium(Stadium stadium) => _stadiumRepo.Delete(stadium);

    public void AddMatch(Match match) => _matchRepo.Add(match);
    public void UpdateMatch(Match match) => _matchRepo.Update(match);
    public void DeleteMatch(Match match) => _matchRepo.Delete(match);

    /// <summary>
    /// Returns the team that owns <paramref name="playerKey"/>, or null if
    /// the player is unaffiliated.
    /// </summary>
    public Team? GetTeamForPlayer(Guid playerKey) =>
        _teamRepo.GetAll().FirstOrDefault(t => t.Players.Any(p => p.Key == playerKey));

    // ---------------------------------------------------------------------
    //  Dynamic Ticket Pricing
    // ---------------------------------------------------------------------

    /// <summary>
    /// Calculate the ticket price for an upcoming or scheduled match.
    /// </summary>
    /// <remarks>
    /// Algorithm (per the spec):
    ///   * if BOTH teams are top-3 by LastSeasonRating  -> highest stadium price
    ///   * if BOTH teams are bottom-3 by LastSeasonRating -> lowest stadium price
    ///   * otherwise -> average stadium price
    /// "Highest / lowest" are computed across the stadiums that the home team
    /// owns (Team1.Stadiums); if the home team has no stadiums we fall back
    /// to the explicit <see cref="Match.StadiumKey"/> price.
    /// </remarks>
    public decimal CalculateTicketPrice(Team team1, Team team2, Stadium fallbackStadium)
    {
        var allTeams = _teamRepo.GetAll().OrderByDescending(t => t.LastSeasonRating).ToList();
        if (allTeams.Count == 0)
        {
            return fallbackStadium.TicketPrice;
        }

        // C# 14: collection expressions / spreads keep this terse. We deliberately
        // materialise once to avoid repeated enumeration when teams overlap.
        var topThreeKeys = allTeams.Take(3).Select(t => t.Key).ToHashSet();
        var bottomThreeKeys = allTeams.TakeLast(3).Select(t => t.Key).ToHashSet();

        var stadiumPool = team1.Stadiums.Count > 0 ? team1.Stadiums : new List<Stadium> { fallbackStadium };
        var prices = stadiumPool.Select(s => s.TicketPrice).DefaultIfEmpty(fallbackStadium.TicketPrice).ToList();

        bool bothTop = topThreeKeys.Contains(team1.Key) && topThreeKeys.Contains(team2.Key);
        bool bothBottom = bottomThreeKeys.Contains(team1.Key) && bottomThreeKeys.Contains(team2.Key);

        if (bothTop) return prices.Max();
        if (bothBottom) return prices.Min();
        return Math.Round(prices.Average(), 2, MidpointRounding.AwayFromZero);
    }

    // ---------------------------------------------------------------------
    //  Query 1 : match dates for a specified team, its opponents, and scores
    // ---------------------------------------------------------------------

    public IReadOnlyList<TeamMatchSummary> GetMatchesForTeam(Guid teamKey)
    {
        var teamLookup = _teamRepo.GetAll().ToDictionary(t => t.Key, t => t);
        if (!teamLookup.TryGetValue(teamKey, out var team))
        {
            return Array.Empty<TeamMatchSummary>();
        }

        return _matchRepo.GetAll()
            .Where(m => m.Team1Key == teamKey || m.Team2Key == teamKey)
            .OrderBy(m => m.Date)
            .Select(m =>
            {
                var opponentKey = m.Team1Key == teamKey ? m.Team2Key : m.Team1Key;
                teamLookup.TryGetValue(opponentKey, out var opponent);
                return new TeamMatchSummary(
                    Team: team.Name,
                    Opponent: opponent?.Name ?? "(unknown)",
                    Date: m.Date,
                    Score: string.IsNullOrWhiteSpace(m.Score) ? "—" : m.Score);
            })
            .ToList();
    }

    // ---------------------------------------------------------------------
    //  Query 2 : jersey numbers + last names for players in teams playing
    //  on a given date at a given stadium.
    // ---------------------------------------------------------------------

    public IReadOnlyList<PlayerLineupRow> GetPlayersInMatchOnDateAtStadium(DateTime date, Guid stadiumKey)
    {
        var match = _matchRepo.GetAll()
            .FirstOrDefault(m =>
                m.StadiumKey == stadiumKey &&
                m.Date.Date == date.Date);

        if (match is null)
        {
            return Array.Empty<PlayerLineupRow>();
        }

        var teams = _teamRepo.GetAll()
            .Where(t => t.Key == match.Team1Key || t.Key == match.Team2Key)
            .ToList();

        return (from t in teams
                from p in t.Players
                orderby t.Name, p.Number
                select new PlayerLineupRow(
                    Team: t.Name,
                    Number: p.Number,
                    LastName: ExtractLastName(p.FullName)))
               .ToList();
    }

    // ---------------------------------------------------------------------
    //  Query 3 : ticket price for a match between two specified teams.
    // ---------------------------------------------------------------------

    public decimal GetTicketPriceForTeams(Guid team1Key, Guid team2Key)
    {
        var teams = _teamRepo.GetAll().ToList();
        var team1 = teams.FirstOrDefault(t => t.Key == team1Key)
            ?? throw new InvalidOperationException("Home team not found.");
        var team2 = teams.FirstOrDefault(t => t.Key == team2Key)
            ?? throw new InvalidOperationException("Away team not found.");

        // Try to use a real scheduled match between these teams; otherwise
        // pick any stadium (preferring the home team's first one).
        var stadium =
            FindStadiumFor(team1, team2)
            ?? team1.Stadiums.FirstOrDefault()
            ?? _stadiumRepo.GetAll().FirstOrDefault()
            ?? throw new InvalidOperationException("No stadium available to price the match.");

        return CalculateTicketPrice(team1, team2, stadium);
    }

    private Stadium? FindStadiumFor(Team team1, Team team2)
    {
        var match = _matchRepo.GetAll()
            .FirstOrDefault(m =>
                (m.Team1Key == team1.Key && m.Team2Key == team2.Key) ||
                (m.Team1Key == team2.Key && m.Team2Key == team1.Key));
        if (match is null) return null;
        return _stadiumRepo.GetByKey(match.StadiumKey);
    }

    // ---------------------------------------------------------------------
    //  Query 4 : best & worst goal difference.
    // ---------------------------------------------------------------------

    public GoalDifferenceReport GetBestAndWorstGoalDifference()
    {
        var matches = _matchRepo.GetAll()
            .Where(m => TryParseScore(m.Score, out _, out _))
            .ToList();

        var standings = _teamRepo.GetAll()
            .Select(t => new TeamGoalDifference(
                t.Name,
                ComputeGoalDifference(t.Key, matches)))
            .OrderByDescending(s => s.GoalDifference)
            .ToList();

        return new GoalDifferenceReport(
            Best: standings.FirstOrDefault() ?? new TeamGoalDifference("(no teams)", 0),
            Worst: standings.LastOrDefault() ?? new TeamGoalDifference("(no teams)", 0),
            All: standings);
    }

    private static int ComputeGoalDifference(Guid teamKey, IEnumerable<Match> matches)
    {
        int diff = 0;
        foreach (var m in matches)
        {
            if (!TryParseScore(m.Score, out var home, out var away)) continue;
            if (m.Team1Key == teamKey) diff += home - away;
            else if (m.Team2Key == teamKey) diff += away - home;
        }
        return diff;
    }

    // ---------------------------------------------------------------------
    //  Query 5 : prize-winning teams (top 3 by points).
    //   Standard football scoring: win = 3, draw = 1, loss = 0.
    // ---------------------------------------------------------------------

    public IReadOnlyList<TeamStandingRow> GetPrizeWinningTeams(int top = 3)
    {
        var matches = _matchRepo.GetAll()
            .Where(m => TryParseScore(m.Score, out _, out _))
            .ToList();

        return _teamRepo.GetAll()
            .Select(t => BuildStanding(t, matches))
            .OrderByDescending(s => s.Points)
            .ThenByDescending(s => s.GoalDifference)
            .ThenBy(s => s.Team)
            .Take(top)
            .ToList();
    }

    private static TeamStandingRow BuildStanding(Team team, IEnumerable<Match> playedMatches)
    {
        int points = 0, played = 0, won = 0, drawn = 0, lost = 0, gf = 0, ga = 0;
        foreach (var m in playedMatches)
        {
            if (!TryParseScore(m.Score, out var home, out var away)) continue;
            int forGoals, againstGoals;
            if (m.Team1Key == team.Key)
            {
                forGoals = home; againstGoals = away;
            }
            else if (m.Team2Key == team.Key)
            {
                forGoals = away; againstGoals = home;
            }
            else
            {
                continue;
            }

            played++;
            gf += forGoals; ga += againstGoals;
            if (forGoals > againstGoals) { won++; points += 3; }
            else if (forGoals == againstGoals) { drawn++; points += 1; }
            else { lost++; }
        }

        return new TeamStandingRow(
            Team: team.Name,
            Played: played,
            Won: won,
            Drawn: drawn,
            Lost: lost,
            GoalDifference: gf - ga,
            Points: points);
    }

    // ---------------------------------------------------------------------
    //  Query 6 : match schedule grouped by stadium.
    // ---------------------------------------------------------------------

    public IReadOnlyList<StadiumScheduleGroup> GetScheduleByStadium()
    {
        var teams = _teamRepo.GetAll().ToDictionary(t => t.Key, t => t);
        var stadiums = _stadiumRepo.GetAll().ToDictionary(s => s.Key, s => s);

        return _matchRepo.GetAll()
            .GroupBy(m => m.StadiumKey)
            .Select(g => new StadiumScheduleGroup(
                Stadium: stadiums.TryGetValue(g.Key, out var s) ? s.Name : "(unknown stadium)",
                Matches: g.OrderBy(m => m.Date)
                          .Select(m => new ScheduledMatchRow(
                              Date: m.Date,
                              Team1: teams.TryGetValue(m.Team1Key, out var t1) ? t1.Name : "(?)",
                              Team2: teams.TryGetValue(m.Team2Key, out var t2) ? t2.Name : "(?)",
                              Score: string.IsNullOrWhiteSpace(m.Score) ? "—" : m.Score))
                          .ToList()))
            .OrderBy(g => g.Stadium)
            .ToList();
    }

    // ---------------------------------------------------------------------
    //  Helpers
    // ---------------------------------------------------------------------

    /// <summary>
    /// Parse a "home:away" score string, also accepting the "home-away"
    /// variant used by some news feeds. Whitespace is tolerated.
    /// </summary>
    public static bool TryParseScore(string? score, out int home, out int away)
    {
        home = away = 0;
        if (string.IsNullOrWhiteSpace(score)) return false;

        var parts = score.Split(new[] { ':', '-', '–' }, 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2) return false;

        return int.TryParse(parts[0], out home) && int.TryParse(parts[1], out away);
    }

    private static string ExtractLastName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
        var pieces = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return pieces.Length == 0 ? fullName : pieces[^1];
    }
}

// ---------------------------------------------------------------------------
//  Report DTOs (kept in the same file because they're tightly coupled to
//  the queries above and have no behaviour of their own).
// ---------------------------------------------------------------------------

public record TeamMatchSummary(string Team, string Opponent, DateTime Date, string Score);

public record PlayerLineupRow(string Team, int Number, string LastName);

public record TeamGoalDifference(string Team, int GoalDifference);

public record GoalDifferenceReport(
    TeamGoalDifference Best,
    TeamGoalDifference Worst,
    IReadOnlyList<TeamGoalDifference> All);

public record TeamStandingRow(
    string Team, int Played, int Won, int Drawn, int Lost, int GoalDifference, int Points);

public record ScheduledMatchRow(DateTime Date, string Team1, string Team2, string Score);

public record StadiumScheduleGroup(string Stadium, IReadOnlyList<ScheduledMatchRow> Matches);
