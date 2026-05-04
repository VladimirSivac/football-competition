using System;

namespace FootballCompetition.Data;

/// <summary>
/// Identifies an entity that exposes a primary <see cref="Key"/> of type
/// <see cref="Guid"/>. The generic <see cref="FileRepository{T}"/> uses this
/// to locate records during update / delete operations.
/// </summary>
public interface IKeyedEntity
{
    Guid Key { get; set; }
}
