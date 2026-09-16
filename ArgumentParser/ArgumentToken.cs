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

    internal ArgumentToken(string text, bool wasQuoted)
    {
        Text = text;
        WasQuoted = wasQuoted;
    }
}
