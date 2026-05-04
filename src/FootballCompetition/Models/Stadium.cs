using System;
using FootballCompetition.Data;

namespace FootballCompetition.Models;

/// <summary>
/// Represents a stadium that may host home matches for one or more teams.
/// </summary>
public class Stadium : IKeyedEntity
{
    public Guid Key { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal TicketPrice { get; set; }

    public override string ToString() => $"{Name} ({City})";
}
