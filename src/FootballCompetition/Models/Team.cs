using System;
using System.Collections.Generic;
using FootballCompetition.Data;

namespace FootballCompetition.Models;

/// <summary>
/// Represents a football team participating in the national competition.
/// </summary>
public class Team : IKeyedEntity
{
    public Guid Key { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Coach { get; set; } = string.Empty;
    public int LastSeasonRating { get; set; }
    public List<Player> Players { get; set; } = new();
    public List<Stadium> Stadiums { get; set; } = new();

    public override string ToString() => Name;
}
