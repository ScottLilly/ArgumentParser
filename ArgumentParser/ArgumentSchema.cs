using System.Collections.ObjectModel;
using System.Globalization;

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
    #region Private variables

    private readonly Dictionary<string, OptionDefinition> _optionsByName;
    private readonly string[] _argSeparators;
    private readonly string[] _keyValueSeparators;
    private readonly string[] _optionPrefixes;
    private readonly IEqualityComparer<string> _comparer;
    private readonly OptionDefinition? _helpOption;

    #endregion

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
        _keyValueSeparators = keyValueSeparators;
        _optionPrefixes = optionPrefixes;
        _comparer = comparer;
        _helpOption = helpOption;

        ApplicationName = applicationName;
        Description = description;
        Usage = usage
            ?? (string.IsNullOrWhiteSpace(applicationName)
                ? null
                : applicationName + " [options]");

        _optionsByName = new Dictionary<string, OptionDefinition>(comparer);

        foreach (OptionDefinition option in Options)
        {
            foreach (string name in option.AllNames())
            {
                _optionsByName.Add(name, option);
            }
        }
    }

    /// <summary>
    /// Starts declaring a schema.
    /// </summary>
    public static IArgumentSchemaBuilder Create() => new ArgumentSchemaBuilder();

    #region Public Methods

    /// <summary>
    /// Parses an array of arguments, such as the one handed to Main, against the declared
    /// options. A null array means no arguments, and null elements are ignored. Each
    /// element is one argument as the shell already tokenized it, so a value containing
    /// whitespace, a double quote, or nothing at all survives as given. An element of
    /// "--name=" is an option with its value left out, the same as on a command line;
    /// an explicitly empty value is its own element.
    /// </summary>
    public SchemaParseResult Parse(string?[]? args) =>
        ParseTokens((args ?? new string?[0])
            .OfType<string>()
            .Select(arg => new ArgumentToken(arg.Trim(), false))
            .ToArray());

    /// <summary>
    /// Parses a command line against the declared options. A null string means no
    /// arguments. Because the options are declared, an option that takes a value can be
    /// written either way round: "--timeout 60" and "--timeout=60" both work, and a flag
    /// needs no value at all.
    /// </summary>
    public SchemaParseResult Parse(string? arguments) =>
        ParseTokens(ArgumentTokenizer
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

    #endregion

    #region Internal Methods

    /// <summary>
    /// The option answering to the name, or null if none does. Returning the option rather
    /// than a bool with an out parameter lets a caller's null check narrow the type, so
    /// none of them needs a null-forgiving operator.
    /// </summary>
    internal OptionDefinition? FindOption(string? name) =>
        name != null && _optionsByName.TryGetValue(name, out OptionDefinition? option)
            ? option
            : null;

    #endregion

    #region Private Methods

    private SchemaParseResult ParseTokens(ArgumentToken[] tokens)
    {
        List<ParseError> errors = new List<ParseError>();
        List<string> positionalArguments = new List<string>();
        Dictionary<string, List<string>> rawValues =
            new Dictionary<string, List<string>>(_comparer);

        ReadTokens(tokens, rawValues, positionalArguments, errors);

        // Asking for help wins over everything else. Without this, "myapp --help" on an
        // application with a required option would report that option as missing, which
        // is not a useful answer to someone asking what the options are.
        bool helpRequested =
            _helpOption != null && rawValues.ContainsKey(_helpOption.Name);

        if (helpRequested)
        {
            return new SchemaParseResult(this,
                new Dictionary<string, IReadOnlyList<object>>(_comparer),
                positionalArguments, new List<ParseError>(), _comparer, true);
        }

        CheckRepeats(rawValues, errors);
        CheckRequired(rawValues, errors);

        Dictionary<string, IReadOnlyList<object>> values =
            ConvertValues(rawValues, errors);

        return new SchemaParseResult(
            this, values, positionalArguments, errors, _comparer, false);
    }

    private void ReadTokens(ArgumentToken[] tokens,
        Dictionary<string, List<string>> rawValues, List<string> positionalArguments,
        List<ParseError> errors)
    {
        for (int index = 0; index < tokens.Length; index++)
        {
            string token = tokens[index].Text;

            // "--timeout=60" and "--timeout:60". The name has to be declared, so a value
            // that merely contains a separator ("C:\build") is not mistaken for one.
            if (ArgumentTokenizer.TrySplitKeyValue(token, _keyValueSeparators,
                    out string inlineName, out string inlineValue))
            {
                OptionDefinition? inlineOption = FindOption(inlineName);

                if (inlineOption != null)
                {
                    // "--timeout=" left the value out. "--name=\"\"" gave an empty one on
                    // purpose, and the quotes are the only way to tell the two apart.
                    if (inlineValue.Length == 0 && !tokens[index].WasQuoted)
                    {
                        errors.Add(MissingValue(inlineOption, inlineName));

                        continue;
                    }

                    Record(rawValues, inlineOption, inlineValue);

                    continue;
                }

                if (IsOptionShaped(inlineName))
                {
                    errors.Add(UnknownOption(inlineName));

                    continue;
                }
            }

            // "--timeout 60", which only works because the declaration says the option
            // takes a value. A flag is true by its presence and consumes nothing.
            OptionDefinition? option = FindOption(token);

            if (option != null)
            {
                if (option.IsFlag)
                {
                    Record(rawValues, option, bool.TrueString);

                    continue;
                }

                // The next argument being another option means the value was left out,
                // rather than that the option should swallow it. "--output --verbose"
                // is a mistake worth reporting, not a request to write to a file
                // called "--verbose". Use "--output=-x" for a value that looks like one.
                if (index + 1 >= tokens.Length || IsOptionShaped(tokens[index + 1].Text))
                {
                    errors.Add(MissingValue(option, token));

                    continue;
                }

                index++;

                Record(rawValues, option, tokens[index].Text);

                continue;
            }

            if (IsOptionShaped(token))
            {
                errors.Add(UnknownOption(token));

                continue;
            }

            positionalArguments.Add(token);
        }
    }

    private static SchemaParseResult Throwing(SchemaParseResult result)
    {
        if (!result.Success)
        {
            throw new ArgumentParseException(result.Errors);
        }

        return result;
    }

    private static void Record(Dictionary<string, List<string>> rawValues,
        OptionDefinition option, string value)
    {
        if (!rawValues.TryGetValue(option.Name, out List<string>? values))
        {
            values = new List<string>();

            rawValues.Add(option.Name, values);
        }

        values.Add(value);
    }

    private void CheckRepeats(Dictionary<string, List<string>> rawValues,
        List<ParseError> errors)
    {
        foreach (OptionDefinition option in Options)
        {
            if (option.IsRepeatable
                || !rawValues.TryGetValue(option.Name, out List<string>? values)
                || values.Count < 2)
            {
                continue;
            }

            errors.Add(new ParseError(ParseErrorKind.OptionNotRepeatable, option.Name, null,
                $"Option '{option.Name}' was given {values.Count} times, but takes one value."));
        }
    }

    private void CheckRequired(Dictionary<string, List<string>> rawValues,
        List<ParseError> errors)
    {
        foreach (OptionDefinition option in Options)
        {
            if (!option.IsRequired || rawValues.ContainsKey(option.Name))
            {
                continue;
            }

            errors.Add(new ParseError(ParseErrorKind.MissingRequiredOption, option.Name, null,
                $"Option '{option.Name}' is required."));
        }
    }

    private Dictionary<string, IReadOnlyList<object>> ConvertValues(
        Dictionary<string, List<string>> rawValues, List<ParseError> errors)
    {
        Dictionary<string, IReadOnlyList<object>> converted =
            new Dictionary<string, IReadOnlyList<object>>(_comparer);

        foreach (KeyValuePair<string, List<string>> entry in rawValues)
        {
            OptionDefinition option = _optionsByName[entry.Key];
            List<object> optionValues = new List<object>();

            foreach (string raw in entry.Value)
            {
                object? value = OptionValueConverter.Convert(raw, option.ValueType);

                if (value != null)
                {
                    optionValues.Add(value);

                    continue;
                }

                errors.Add(new ParseError(ParseErrorKind.UnconvertibleValue, option.Name, raw,
                    $"Option '{option.Name}' needs {DescribeType(option.ValueType)}, but was given '{raw}'."));
            }

            converted.Add(entry.Key, new ReadOnlyCollection<object>(optionValues));
        }

        return converted;
    }

    private static string DescribeType(Type type)
    {
        if (type.IsEnum)
        {
            return "one of " + string.Join("|", Enum.GetNames(type));
        }

        if (type == typeof(int) || type == typeof(long))
        {
            return "a whole number";
        }

        if (type == typeof(decimal) || type == typeof(double))
        {
            return "a number";
        }

        if (type == typeof(bool))
        {
            return "true or false";
        }

        return "a " + type.Name.ToLowerInvariant();
    }

    private ParseError UnknownOption(string name) =>
        new ParseError(ParseErrorKind.UnknownOption, name, null,
            $"Unknown option '{name}'.");

    // Named by the option as the user typed it, so "-o" is reported as "-o" and not as
    // "--output".
    private static ParseError MissingValue(OptionDefinition option, string nameAsTyped) =>
        new ParseError(ParseErrorKind.MissingValue, option.Name, null,
            $"Option '{nameAsTyped}' needs a value.");

    /// <summary>
    /// True when the argument looks like an option name rather than a value, which is what
    /// lets an undeclared "--skip-semantc" be reported instead of silently ignored. A
    /// negative number is not an option, however much it looks like one.
    /// </summary>
    private bool IsOptionShaped(string token)
    {
        bool hasPrefix = _optionPrefixes.Any(prefix =>
            token.Length > prefix.Length
            && token.StartsWith(prefix, StringComparison.Ordinal));

        if (!hasPrefix)
        {
            return false;
        }

        return !int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
            && !decimal.TryParse(token,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out _);
    }

    #endregion
}
