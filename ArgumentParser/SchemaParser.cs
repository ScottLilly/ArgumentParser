using System.Collections.ObjectModel;
using System.Globalization;

namespace ArgumentParser;

/// <summary>
/// Reads a command line against a schema's declared options. This is everything that
/// happens between ArgumentSchema.Parse being called and a SchemaParseResult coming back:
/// matching each token to a declaration, collecting the raw values, checking what was
/// required and what may repeat, and converting the values to their declared types.
/// <para>
/// It lives apart from ArgumentSchema because a schema is a declaration, and reading a
/// command line against it is a different job that happens to need one.
/// </para>
/// </summary>
internal sealed class SchemaParser
{
    private readonly ArgumentSchema _schema;
    private readonly Dictionary<string, OptionDefinition> _optionsByName;
    private readonly string[] _keyValueSeparators;
    private readonly string[] _optionPrefixes;
    private readonly IEqualityComparer<string> _comparer;
    private readonly OptionDefinition? _helpOption;

    internal SchemaParser(ArgumentSchema schema, string[] keyValueSeparators,
        string[] optionPrefixes, IEqualityComparer<string> comparer,
        OptionDefinition? helpOption)
    {
        _schema = schema;
        _keyValueSeparators = keyValueSeparators;
        _optionPrefixes = optionPrefixes;
        _comparer = comparer;
        _helpOption = helpOption;

        _optionsByName = new Dictionary<string, OptionDefinition>(comparer);

        foreach (OptionDefinition option in schema.Options)
        {
            foreach (string name in option.AllNames())
            {
                _optionsByName.Add(name, option);
            }
        }
    }

    /// <summary>
    /// The option answering to the name, or null if none does. Returning the option rather
    /// than a bool with an out parameter lets a caller's null check narrow the type, so
    /// none of them needs a null-forgiving operator.
    /// </summary>
    internal OptionDefinition? FindOption(string? name) =>
        name != null && _optionsByName.TryGetValue(name, out OptionDefinition? option)
            ? option
            : null;

    internal SchemaParseResult Parse(ArgumentToken[] tokens)
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
            return new SchemaParseResult(_schema,
                new Dictionary<string, IReadOnlyList<object>>(_comparer),
                positionalArguments, new List<ParseError>(), _comparer, true);
        }

        CheckRepeats(rawValues, errors);
        CheckRequired(rawValues, errors);

        Dictionary<string, IReadOnlyList<object>> values =
            ConvertValues(rawValues, errors);

        return new SchemaParseResult(
            _schema, values, positionalArguments, errors, _comparer, false);
    }

    private void ReadTokens(ArgumentToken[] tokens,
        Dictionary<string, List<string>> rawValues, List<string> positionalArguments,
        List<ParseError> errors)
    {
        bool endOfOptions = false;

        for (int index = 0; index < tokens.Length; index++)
        {
            string token = tokens[index].Text;

            // A bare "--" ends the options: everything after it is positional, whatever it
            // looks like. That is the only way to pass a positional argument that starts
            // with an option prefix.
            if (!endOfOptions && IsEndOfOptionsMarker(token))
            {
                endOfOptions = true;

                continue;
            }

            if (endOfOptions)
            {
                positionalArguments.Add(token);

                continue;
            }

            if (ConsumedAsInlineValue(tokens[index], rawValues, errors))
            {
                continue;
            }

            if (ConsumedAsSeparateValue(tokens, ref index, rawValues, errors))
            {
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

    /// <summary>
    /// Reads "--timeout=60" and "--timeout:60", where the value is part of the same token.
    /// The name has to be declared, so a value that merely contains a separator
    /// ("C:\build") is not mistaken for one. True means the token was consumed as this
    /// form, which includes the malformed cases that record an error instead of a value.
    /// False means the token was not one of these and the caller should go on looking at
    /// it.
    /// </summary>
    private bool ConsumedAsInlineValue(ArgumentToken token,
        Dictionary<string, List<string>> rawValues, List<ParseError> errors)
    {
        if (!ArgumentTokenizer.TrySplitKeyValue(token.Text, _keyValueSeparators,
                out string name, out string value))
        {
            return false;
        }

        OptionDefinition? option = FindOption(name);

        if (option != null)
        {
            // "--timeout=" left the value out. "--name=\"\"" gave an empty one on purpose,
            // and the quotes are the only way to tell the two apart.
            if (value.Length == 0 && !token.WasQuoted)
            {
                errors.Add(MissingValue(option, name));

                return true;
            }

            Record(rawValues, option, value);

            return true;
        }

        if (IsOptionShaped(name))
        {
            errors.Add(UnknownOption(name));

            return true;
        }

        return false;
    }

    /// <summary>
    /// Reads "--timeout 60", which only works because the declaration says the option takes
    /// a value, and the flag that is true by its presence and consumes nothing. Advances
    /// the index past the value when one is taken. True means the token was consumed as
    /// this form, which includes a declared option whose value was left out and so
    /// records an error instead. False means the token is not a declared option name.
    /// </summary>
    private bool ConsumedAsSeparateValue(ArgumentToken[] tokens, ref int index,
        Dictionary<string, List<string>> rawValues, List<ParseError> errors)
    {
        string token = tokens[index].Text;
        OptionDefinition? option = FindOption(token);

        if (option == null)
        {
            return false;
        }

        if (option.IsFlag)
        {
            Record(rawValues, option, bool.TrueString);

            return true;
        }

        // The next argument being another option means the value was left out, rather
        // than that the option should swallow it. "--output --verbose" is a mistake worth
        // reporting, not a request to write to a file called "--verbose". Use
        // "--output=-x" for a value that looks like one.
        if (index + 1 >= tokens.Length || IsOptionShaped(tokens[index + 1].Text))
        {
            errors.Add(MissingValue(option, token));

            return true;
        }

        index++;

        Record(rawValues, option, tokens[index].Text);

        return true;
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
        foreach (OptionDefinition option in _schema.Options)
        {
            // A flag is never a repeat error. Command line tools tolerate "-v -v", and
            // some read the count as more of whatever the flag asks for, so reporting it
            // would make this parser stricter than the convention it follows. Every
            // occurrence is still recorded, so AllValuesOf<bool> can count them.
            if (option.IsFlag
                || option.IsRepeatable
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
        foreach (OptionDefinition option in _schema.Options)
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
                object? value =
                    OptionValueConverter.Convert(raw, option.ValueType, _comparer);

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

    private static ParseError UnknownOption(string name) =>
        new ParseError(ParseErrorKind.UnknownOption, name, null,
            $"Unknown option '{name}'.");

    // Named by the option as the user typed it, so "-o" is reported as "-o" and not as
    // "--output".
    private static ParseError MissingValue(OptionDefinition option, string nameAsTyped) =>
        new ParseError(ParseErrorKind.MissingValue, option.Name, null,
            $"Option '{nameAsTyped}' needs a value.");

    /// <summary>
    /// True for the bare "--" that ends the options. Tied to the configured prefixes rather
    /// than hard coded, so a schema built with WithOptionPrefixes("/") does not give a
    /// meaning to something its users would never type.
    /// </summary>
    private bool IsEndOfOptionsMarker(string token) =>
        token == "--" && _optionPrefixes.Contains("--", StringComparer.Ordinal);

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
}
