using System;
using FootballCompetition.Data;

namespace FootballCompetition.Models;

/// <summary>
/// Represents an individual player belonging to a single <see cref="Team"/>.
/// </summary>
public class Player : IKeyedEntity
{
    public Guid Key { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Role { get; set; } = string.Empty;
    public int Number { get; set; }

    public override string ToString() => $"#{Number} {FullName} ({Role})";
}
