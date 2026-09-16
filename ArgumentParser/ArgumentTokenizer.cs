using System.Text;

namespace ArgumentParser;

/// <summary>
/// Splits a command line into arguments. Shared by the untyped Parser and the declared
/// schema, so both handle quoting and separator matching the same way.
/// </summary>
internal static class ArgumentTokenizer
{
    /// <summary>
    /// Splits on the argument separators, except where a separator falls inside a pair of
    /// double quotes, so a value containing a separator survives as one argument. The
    /// quotes group the value and are not part of it, so they are removed as it is read,
    /// and each token remembers whether it had any, and which separator it followed.
    /// </summary>
    internal static ArgumentToken[] Split(string arguments, string[] separators)
    {
        List<ArgumentToken> tokens = new List<ArgumentToken>();
        StringBuilder current = new StringBuilder();
        bool insideQuotes = false;
        bool quoted = false;
        string? precedingSeparator = null;
        int index = 0;

        while (index < arguments.Length)
        {
            if (arguments[index] == '"')
            {
                insideQuotes = !insideQuotes;
                quoted = true;
                index++;

                continue;
            }

            string? separator =
                insideQuotes ? null : MatchSeparatorAt(arguments, index, separators);

            if (separator == null)
            {
                current.Append(arguments[index]);
                index++;

                continue;
            }

            AddToken(tokens, current, quoted, precedingSeparator);
            quoted = false;
            precedingSeparator = separator;
            index += separator.Length;
        }

        AddToken(tokens, current, quoted, precedingSeparator);

        return tokens.ToArray();
    }

    // A run of separators, or a separator at either end, contributes nothing. A quoted
    // empty string was typed deliberately, so it is kept.
    private static void AddToken(List<ArgumentToken> tokens, StringBuilder current,
        bool quoted, string? precedingSeparator)
    {
        string text = current.ToString();

        current.Clear();

        if (text.Length > 0 || quoted)
        {
            tokens.Add(new ArgumentToken(text, quoted, precedingSeparator));
        }
    }

    /// <summary>
    /// Restores the quoting the shell removed before Main saw the argument. Only whitespace
    /// forces it, because whitespace is what the shell tokenized on. A separator such as
    /// "--" is left alone, since the parser is meant to strip that as a prefix.
    /// </summary>
    internal static string? Requote(string? arg)
    {
        if (arg == null || arg.IndexOf('"') >= 0 || !arg.Any(char.IsWhiteSpace))
        {
            return arg;
        }

        return "\"" + arg + "\"";
    }

    /// <summary>
    /// Splits an argument at the leftmost key/value separator. Leftmost rather than first
    /// in the configured order, so a value containing a later separator ("--out=C:\build")
    /// splits at the separator the caller actually typed.
    /// </summary>
    internal static bool TrySplitKeyValue(string argument, string[] separators,
        out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;

        int splitIndex = -1;
        int splitLength = 0;

        foreach (string separator in separators)
        {
            int separatorIndex = argument.IndexOf(separator, StringComparison.Ordinal);

            if (separatorIndex >= 1 && (splitIndex == -1 || separatorIndex < splitIndex))
            {
                splitIndex = separatorIndex;
                splitLength = separator.Length;
            }
        }

        if (splitIndex == -1)
        {
            return false;
        }

        key = argument.Substring(0, splitIndex);
        value = argument.Substring(splitIndex + splitLength).Trim();

        return true;
    }

    /// <summary>
    /// Returns the separator starting at index, or null if none does. The longest match
    /// wins, so "--" is preferred over "-" however the separators were ordered.
    /// </summary>
    private static string? MatchSeparatorAt(string arguments, int index, string[] separators)
    {
        string? longestMatch = null;

        foreach (string separator in separators)
        {
            if (separator.Length == 0
                || (longestMatch != null && separator.Length <= longestMatch.Length))
            {
                continue;
            }

            if (index + separator.Length <= arguments.Length
                && string.CompareOrdinal(arguments, index, separator, 0, separator.Length) == 0)
            {
                longestMatch = separator;
            }
        }

        return longestMatch;
    }
}
