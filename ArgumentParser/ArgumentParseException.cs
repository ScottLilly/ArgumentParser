using System.Collections.ObjectModel;

namespace ArgumentParser;

/// <summary>
/// Thrown by ArgumentSchema.ParseOrThrow when the arguments do not match the schema.
/// Carries every error, not just the first, so a caller catching it can still show the
/// whole list.
/// </summary>
public class ArgumentParseException : Exception
{
    /// <summary>
    /// Every problem found with the arguments.
    /// </summary>
    public IReadOnlyList<ParseError> Errors { get; }

    internal ArgumentParseException(IEnumerable<ParseError> errors)
        : base(string.Join(Environment.NewLine, errors.Select(e => e.Message)))
    {
        Errors = new ReadOnlyCollection<ParseError>(errors.ToList());
    }
}
