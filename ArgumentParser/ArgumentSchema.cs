using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace ArgumentParser
{
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
    public class ArgumentSchema
    {
        #region Private variables

        private readonly Dictionary<string, OptionDefinition> _optionsByName;
        private readonly string[] _argSeparators;
        private readonly string[] _keyValueSeparators;
        private readonly string[] _optionPrefixes;
        private readonly IEqualityComparer<string> _comparer;

        #endregion

        /// <summary>
        /// Every declared option, in the order it was declared.
        /// </summary>
        public IReadOnlyList<OptionDefinition> Options { get; }

        internal ArgumentSchema(IEnumerable<OptionDefinition> options, string[] argSeparators,
            string[] keyValueSeparators, string[] optionPrefixes,
            IEqualityComparer<string> comparer)
        {
            Options = new ReadOnlyCollection<OptionDefinition>(options.ToList());
            _argSeparators = argSeparators;
            _keyValueSeparators = keyValueSeparators;
            _optionPrefixes = optionPrefixes;
            _comparer = comparer;

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
        /// Parses an array of arguments against the declared options. A null array means no
        /// arguments. An element containing whitespace is requoted before the join, so a path
        /// the shell already unquoted survives as one argument.
        /// </summary>
        public SchemaParseResult Parse(string[] args) =>
            Parse(args == null
                ? string.Empty
                : string.Join(" ", args.Select(ArgumentTokenizer.Requote)));

        /// <summary>
        /// Parses a command line against the declared options. A null string means no
        /// arguments. Because the options are declared, an option that takes a value can be
        /// written either way round: "--timeout 60" and "--timeout=60" both work, and a flag
        /// needs no value at all.
        /// </summary>
        public SchemaParseResult Parse(string arguments)
        {
            string[] tokens = ArgumentTokenizer
                .Split(arguments ?? string.Empty, _argSeparators)
                .Select(token => token.Trim())
                .Where(token => token.Length > 0)
                .ToArray();

            List<ParseError> errors = new List<ParseError>();
            List<string> positionalArguments = new List<string>();
            Dictionary<string, List<string>> rawValues =
                new Dictionary<string, List<string>>(_comparer);

            ReadTokens(tokens, rawValues, positionalArguments, errors);
            CheckRepeats(rawValues, errors);
            CheckRequired(rawValues, errors);

            Dictionary<string, IReadOnlyList<object>> values =
                ConvertValues(rawValues, errors);

            return new SchemaParseResult(
                this, values, positionalArguments, errors, _comparer);
        }

        /// <summary>
        /// Parses against the declared options, throwing if anything is wrong instead of
        /// returning a result to check. For callers who would rather handle one exception than
        /// test Success. The exception carries every error, not only the first.
        /// </summary>
        /// <exception cref="ArgumentParseException">The arguments do not match the schema.</exception>
        public SchemaParseResult ParseOrThrow(string arguments) => Throwing(Parse(arguments));

        /// <summary>
        /// Parses against the declared options, throwing if anything is wrong instead of
        /// returning a result to check.
        /// </summary>
        /// <exception cref="ArgumentParseException">The arguments do not match the schema.</exception>
        public SchemaParseResult ParseOrThrow(string[] args) => Throwing(Parse(args));

        #endregion

        #region Internal Methods

        internal bool TryFindOption(string name, out OptionDefinition option)
        {
            option = null;

            return name != null && _optionsByName.TryGetValue(name, out option);
        }

        #endregion

        #region Private Methods

        private void ReadTokens(string[] tokens, Dictionary<string, List<string>> rawValues,
            List<string> positionalArguments, List<ParseError> errors)
        {
            for (int index = 0; index < tokens.Length; index++)
            {
                string token = tokens[index];

                // "--timeout=60" and "--timeout:60". The name has to be declared, so a value
                // that merely contains a separator ("C:\build") is not mistaken for one.
                if (ArgumentTokenizer.TrySplitKeyValue(token, _keyValueSeparators,
                        out string inlineName, out string inlineValue))
                {
                    if (TryFindOption(inlineName, out OptionDefinition inlineOption))
                    {
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
                if (TryFindOption(token, out OptionDefinition option))
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
                    if (index + 1 >= tokens.Length || IsOptionShaped(tokens[index + 1]))
                    {
                        errors.Add(new ParseError(ParseErrorKind.MissingValue, option.Name, null,
                            $"Option '{token}' needs a value."));

                        continue;
                    }

                    index++;

                    Record(rawValues, option, tokens[index]);

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
            if (!rawValues.TryGetValue(option.Name, out List<string> values))
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
                    || !rawValues.TryGetValue(option.Name, out List<string> values)
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
                    if (OptionValueConverter.TryConvert(raw, option.ValueType, out object value))
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
                return "one of " + string.Join(", ", Enum.GetNames(type));
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
}
