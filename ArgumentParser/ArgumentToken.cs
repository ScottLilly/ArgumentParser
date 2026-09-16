namespace ArgumentParser;

/// <summary>
/// One argument from a split command line, and whether any of it was quoted. The quotes
/// themselves are gone by this point, so this is what tells an empty value that was typed
/// deliberately ("--name=\"\"") from one that was left out ("--name=").
/// </summary>
internal readonly struct ArgumentToken
{
    internal string Text { get; }

    internal bool WasQuoted { get; }

    /// <summary>
    /// The argument separator that ended the previous token, or null for the first token in
    /// the command line. This is how a prefix that was consumed by the split is remembered:
    /// with "--" configured as an argument separator, "--solution:a" arrives here as
    /// "solution:a" preceded by "--", and the key still counts as prefixed.
    /// </summary>
    internal string? PrecedingSeparator { get; }

    internal ArgumentToken(string text, bool wasQuoted, string? precedingSeparator = null)
    {
        Text = text;
        WasQuoted = wasQuoted;
        PrecedingSeparator = precedingSeparator;
    }
}
