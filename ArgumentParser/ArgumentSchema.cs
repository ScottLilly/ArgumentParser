using System.Collections.ObjectModel;

namespace ArgumentParser;

/// <summary>
/// A set of declared options, and the parser that reads a command line against them.
/// <para>
/// This sits alongside the untyped Parser rather than replacing it. Parser classifies
/// whatever it finds and is the right tool for a free-form string. A schema is for an
/// application that knows what its options are, and gets unknown option detection, type
/// conversion, required checks, defaults, aliases and repeatable options in exchange for
/// declaring them.
/// </para>
/// </summary>
/// <example>
/// <code>
/// ArgumentSchema schema =
///     ArgumentSchema.Create()
///         .Option&lt;string&gt;("--output", alias: "-o", required: true, repeatable: true)
///         .Option&lt;int&gt;("--timeout", defaultValue: 60)
///         .Flag("--verbose", alias: "-v")
///         .Build();
///
/// SchemaParseResult result = schema.Parse(args);
///
/// if (!result.Success)
/// {
///     Console.Error.WriteLine(result.ErrorText());
///     return 1;
/// }
///
/// int timeout = result.ValueOf&lt;int&gt;("--timeout");
/// </code>
/// </example>
public sealed class ArgumentSchema
{
    private readonly string[] _argSeparators;
    private readonly SchemaParser _parser;

    /// <summary>
    /// Every declared option, in the order it was declared. The automatic help option, if
    /// there is one, is last.
    /// </summary>
    public IReadOnlyList<OptionDefinition> Options { get; }

    /// <summary>
    /// The application's name, for the usage line. Null when none was given.
    /// </summary>
    public string? ApplicationName { get; }

    /// <summary>
    /// A one-line summary of what the application does. Null when none was given.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// The usage line, either as given or built from the application name. Null when
    /// neither was given.
    /// </summary>
    public string? Usage { get; }

    internal ArgumentSchema(IEnumerable<OptionDefinition> options, string[] argSeparators,
        string[] keyValueSeparators, string[] optionPrefixes,
        IEqualityComparer<string> comparer, string? applicationName, string? description,
        string? usage, OptionDefinition? helpOption)
    {
        Options = new ReadOnlyCollection<OptionDefinition>(options.ToList());
        _argSeparators = argSeparators;

        ApplicationName = applicationName;
        Description = description;
        Usage = usage
            ?? (string.IsNullOrWhiteSpace(applicationName)
                ? null
                : applicationName + " [options]");

        // Last, because it indexes Options by name and so needs them in place.
        _parser = new SchemaParser(
            this, keyValueSeparators, optionPrefixes, comparer, helpOption);
    }

    /// <summary>
    /// Starts declaring a schema.
    /// </summary>
    public static IArgumentSchemaBuilder Create() => new ArgumentSchemaBuilder();

    /// <summary>
    /// Parses an array of arguments, such as the one handed to Main, against the declared
    /// options. A null array means no arguments, and null elements are ignored. Each
    /// element is one argument as the shell already tokenized it, so a value containing
    /// whitespace, a double quote, or nothing at all survives as given. An element of
    /// "--name=" is an option with its value left out, the same as on a command line;
    /// an explicitly empty value is its own element. An element of "--" ends the options,
    /// so everything after it is positional.
    /// </summary>
    public SchemaParseResult Parse(string?[]? args) =>
        _parser.Parse((args ?? new string?[0])
            .OfType<string>()
            .Select(arg => new ArgumentToken(arg.Trim(), false))
            .ToArray());

    /// <summary>
    /// Parses a command line against the declared options. A null string means no
    /// arguments. Because the options are declared, an option that takes a value can be
    /// written either way round: "--timeout 60" and "--timeout=60" both work, and a flag
    /// needs no value at all. A bare "--" ends the options, so everything after it is
    /// positional whatever it looks like. Leading and trailing whitespace is trimmed from
    /// every argument and value, quoted or not; whitespace inside a value is left alone.
    /// </summary>
    public SchemaParseResult Parse(string? arguments) =>
        _parser.Parse(ArgumentTokenizer
            .Split(arguments ?? string.Empty, _argSeparators)
            .Select(token => new ArgumentToken(token.Text.Trim(), token.WasQuoted))
            .Where(token => token.Text.Length > 0 || token.WasQuoted)
            .ToArray());

    /// <summary>
    /// The help text, built from the same declarations that parse the arguments, so the
    /// two cannot disagree. Nothing is written anywhere: the caller decides where it goes,
    /// which keeps this testable and usable from something that is not a console.
    /// </summary>
    /// <param name="width">Column to wrap descriptions at. Fixed rather than taken from the console, so redirected output is stable.</param>
    public string HelpText(int width = 80) => HelpTextFormatter.Format(this, width);

    /// <summary>
    /// Parses against the declared options, throwing if anything is wrong instead of
    /// returning a result to check. For callers who would rather handle one exception than
    /// test Success. The exception carries every error, not only the first.
    /// </summary>
    /// <exception cref="ArgumentParseException">The arguments do not match the schema.</exception>
    public SchemaParseResult ParseOrThrow(string? arguments) => Throwing(Parse(arguments));

    /// <summary>
    /// Parses against the declared options, throwing if anything is wrong instead of
    /// returning a result to check.
    /// </summary>
    /// <exception cref="ArgumentParseException">The arguments do not match the schema.</exception>
    public SchemaParseResult ParseOrThrow(string?[]? args) => Throwing(Parse(args));

    /// <summary>
    /// The option answering to the name, or null if none does. The index it looks in is
    /// the parser's, since that is what the names are matched against when arguments are
    /// read.
    /// </summary>
    internal OptionDefinition? FindOption(string? name) => _parser.FindOption(name);

    private static SchemaParseResult Throwing(SchemaParseResult result)
    {
        if (!result.Success)
        {
            throw new ArgumentParseException(result.Errors);
        }

        return result;
    }
}
