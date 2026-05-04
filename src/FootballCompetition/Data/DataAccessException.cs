using System;

namespace FootballCompetition.Data;

/// <summary>
/// Thrown by <see cref="FileRepository{T}"/> when persisting or loading
/// the JSON store fails. The UI layer catches this to surface a friendly
/// message via the global error overlay.
/// </summary>
public class DataAccessException : Exception
{
    public string FilePath { get; }

    public DataAccessException(string message, string filePath)
        : base(message)
    {
        FilePath = filePath;
    }

    public DataAccessException(string message, string filePath, Exception inner)
        : base(message, inner)
    {
        FilePath = filePath;
    }
}
