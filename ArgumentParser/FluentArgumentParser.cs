namespace ArgumentParser;

/// <summary>
/// Builds and runs a Parser through a chain of calls, for callers who find that easier to
/// read than the constructor's optional parameters. Start one with Create.
/// </summary>
/// <example>
/// <code>
/// ParsedArguments parsedArguments =
///     FluentArgumentParser
///         .Create()
///         .AddArgumentSeparators(new[] { "--", "-" })
///         .AddKeyValueSeparators(new[] { ':', '|' })
///         .Parse(@"--solution:value1 -s|value2");
/// </code>
/// </example>
public sealed class FluentArgumentParser : IFluentArgumentParserBuilder
{
    // User HashSets for storing separators, to avoid duplicates
    private readonly HashSet<string> _argumentSeparators = new HashSet<string>();
    private readonly HashSet<char> _keyValueSeparators = new HashSet<char>();
    private IEqualityComparer<string>? _comparer;
    private string[]? _namedArgumentPrefixes;

    // Private, so Create is the only way in. The chaining methods return the interface
    // rather than the class, and nothing works differently if a caller holds the concrete
    // type, so there is no reason for a second entry point that skips the factory.
    private FluentArgumentParser()
    {
    }

    /// <summary>
    /// Starts a chain of calls that ends in Parse.
    /// </summary>
    /// <returns>A builder to add separators to, then parse with.</returns>
    public static IFluentArgumentParserBuilder Create()
    {
        return new FluentArgumentParser();
    }

    /// <inheritdoc />
    public IFluentArgumentParserBuilder AddArgumentSeparator(char argumentSeparator)
    {
        _argumentSeparators.Add(argumentSeparator.ToString());

        return this;
    }

    /// <inheritdoc />
    public IFluentArgumentParserBuilder AddArgumentSeparator(string argumentSeparator)
    {
        _argumentSeparators.Add(argumentSeparator);

        return this;
    }

    /// <inheritdoc />
    public IFluentArgumentParserBuilder AddArgumentSeparators(char[] argumentSeparators)
    {
        foreach (char separator in argumentSeparators)
        {
            _argumentSeparators.Add(separator.ToString());
        }

        return this;
    }

    /// <inheritdoc />
    public IFluentArgumentParserBuilder AddArgumentSeparators(string[] argumentSeparator)
    {
        foreach (string separator in argumentSeparator)
        {
            _argumentSeparators.Add(separator);
        }

        return this;
    }

    /// <inheritdoc />
    public IFluentArgumentParserBuilder AddKeyValueSeparator(char keyValueSeparator)
    {
        _keyValueSeparators.Add(keyValueSeparator);

        return this;
    }

    /// <inheritdoc />
    public IFluentArgumentParserBuilder AddKeyValueSeparators(char[] keyValueSeparators)
    {
        foreach (char separator in keyValueSeparators)
        {
            _keyValueSeparators.Add(separator);
        }

        return this;
    }

    /// <inheritdoc />
    public IFluentArgumentParserBuilder WithComparer(IEqualityComparer<string>? comparer)
    {
        _comparer = comparer;

        return this;
    }

    /// <inheritdoc />
    public IFluentArgumentParserBuilder WithNamedArgumentPrefixes(
        string[]? namedArgumentPrefixes)
    {
        _namedArgumentPrefixes = namedArgumentPrefixes;

        return this;
    }

    /// <inheritdoc />
    public ParsedArguments Parse(string? arguments)
    {
        return BuildParser().Parse(arguments);
    }

    /// <inheritdoc />
    public ParsedArguments Parse(string?[]? arguments)
    {
        return BuildParser().Parse(arguments);
    }

    // A separator set that was never added to is passed as null rather than as an empty
    // array, so Parser falls back to its own defaults. An empty array would silently
    // match nothing, making Create().Parse(...) behave differently from new Parser().
    private Parser BuildParser()
    {
        return new Parser(
            _argumentSeparators.Count > 0 ? _argumentSeparators.ToArray() : null,
            _keyValueSeparators.Count > 0 ? _keyValueSeparators.ToArray() : null,
            _comparer,
            _namedArgumentPrefixes);
    }
}
