using System;
using FootballCompetition.Data;

namespace FootballCompetition.Models;

/// <summary>
/// A scheduled or played match between two teams.
/// </summary>
/// <remarks>
/// We persist team and stadium references by key (Guid) rather than by inline
/// object graph to keep the JSON store normalised and avoid circular nesting.
/// </remarks>
public class Match : IKeyedEntity
{
    public Guid Key { get; set; } = Guid.NewGuid();
    public Guid Team1Key { get; set; }
    public Guid Team2Key { get; set; }
    public Guid StadiumKey { get; set; }
    public DateTime Date { get; set; }

    /// <summary>
    /// Final score in the canonical "home:away" format, e.g. "2:1".
    /// Empty / null means the match has not been played yet.
    /// </summary>
    public string Score { get; set; } = string.Empty;
}
