using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ArgumentParser
{
    /// <summary>
    /// Parser class for parsing command line arguments into a structured format.
    /// </summary>
    public class Parser
    {
        #region Private variables

        // "Constants" for default argument and key/value separators
        private static readonly ReadOnlyCollection<string> s_defaultArgSeparators =
            new ReadOnlyCollection<string>(new string[] { " " });
        private static readonly ReadOnlyCollection<string> s_defaultKeyValueSeparators =
            new ReadOnlyCollection<string>(new string[] { ":", "=" });

        // Command line option names are conventionally case-insensitive on Windows, which is
        // where most consumers of this package run. Pass StringComparer.Ordinal for the
        // case-sensitive matching a Unix style application expects.
        private static readonly IEqualityComparer<string> s_defaultComparer =
            StringComparer.OrdinalIgnoreCase;

        private readonly string[] _argSeparators;
        private readonly string[] _keyValueSeparators;
        private readonly IEqualityComparer<string> _comparer;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the Parser class with default argument and key/value separators.
        /// </summary>
        /// <param name="argSeparators">(Optional) array of characters that indicate a separator between arguments. Default value is { ' ' }</param>
        /// <param name="keyValueSeparators">(Optional) array of charcters that indicate a separator between the key and value in a key/value argument. Default values are { ':', '=' }</param>
        /// <param name="comparer">(Optional) comparer used to match named argument names. Defaults to StringComparer.OrdinalIgnoreCase, so "--output" and "--Output" are the same argument. Pass StringComparer.Ordinal to match names case-sensitively.</param>
        public Parser(char[] argSeparators = null, char[] keyValueSeparators = null,
            IEqualityComparer<string> comparer = null)
        {
            _argSeparators =
                argSeparators?.Select(c => c.ToString()).ToArray()
                ?? s_defaultArgSeparators.ToArray();

            _keyValueSeparators =
                keyValueSeparators?.Select(c => c.ToString()).ToArray()
                ?? s_defaultKeyValueSeparators.ToArray();

            _comparer = comparer ?? s_defaultComparer;
        }

        /// <summary>
        /// Initializes a new instance of the Parser class with default argument and key/value separators.
        /// </summary>
        /// <param name="argSeparators">(Optional) array of strings that indicate a separator between arguments. Default value is { " " }</param>
        /// <param name="keyValueSeparators">(Optional) array of charcters that indicate a separator between the key and value in a key/value argument. Default values are { ':', '=' }</param>
        /// <param name="comparer">(Optional) comparer used to match named argument names. Defaults to StringComparer.OrdinalIgnoreCase, so "--output" and "--Output" are the same argument. Pass StringComparer.Ordinal to match names case-sensitively.</param>
        public Parser(string[] argSeparators, char[] keyValueSeparators = null,
            IEqualityComparer<string> comparer = null)
        {
            _argSeparators =
                argSeparators
                ?? s_defaultArgSeparators.ToArray();

            _keyValueSeparators =
                keyValueSeparators?.Select(c => c.ToString()).ToArray()
                ?? s_defaultKeyValueSeparators.ToArray();

            _comparer = comparer ?? s_defaultComparer;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Parses an array of string arguments into a ParsedArguments object.
        /// A null array means no arguments, and returns an empty ParsedArguments.
        /// Null elements within the array are ignored. An element containing whitespace is
        /// requoted before the join, so a path the shell already unquoted ("My Project.sln")
        /// survives as one argument instead of being split again.
        /// </summary>
        /// <param name="args">Array of string arguments to parse. May be null.</param>
        /// <returns>ParsedArguments object, populate with values from arguments parameter</returns>
        public ParsedArguments Parse(string[] args)
        {
            // Concatenate the array of strings into a single string,
            // before passing to the Parse method that accepts a single string parameter.
            // This is to handle command line arguments that may be intended as key/value pairs,
            // but have already been split into an array by a console app's static Main method.
            return Parse(args == null ? string.Empty : string.Join(" ", args.Select(Requote)));
        }

        /// <summary>
        /// Parses a single string of arguments into a ParsedArguments object.
        /// Numeric arguments are recognized with the invariant culture, so the same command
        /// line classifies identically on every machine. An argument counts as a number only
        /// if it is digits with an optional leading sign and, for decimals, a single period.
        /// Anything else, including group separators and exponents, is a string argument.
        /// A null string means no arguments, and returns an empty ParsedArguments.
        /// A double quoted section is not split on, so a value can contain a separator
        /// ("--solution=\"C:\\My Project.sln\""). The quotes group the value and are removed
        /// from it. There is no escape sequence, so a value cannot contain a double quote.
        /// </summary>
        /// <param name="arguments">String containing arguments to parse. May be null.</param>
        /// <returns>ParsedArguments object, populate with values from arguments parameter</returns>
        public ParsedArguments Parse(string arguments)
        {
            // Null is treated as no arguments rather than as an error, so a Main(string[] args)
            // caller does not have to guard the call. Empty input already returned an empty
            // result, and this makes null agree with it.
            string[] splitArgs = SplitRespectingQuotes(arguments ?? string.Empty);

            List<int> integerArguments = new List<int>();
            List<decimal> decimalArguments = new List<decimal>();
            List<string> stringArguments = new List<string>();
            // Both views use the same comparer, so they always agree on which names are present.
            Dictionary<string, string> namedArguments =
                new Dictionary<string, string>(_comparer);
            Dictionary<string, List<string>> allNamedArgumentValues =
                new Dictionary<string, List<string>>(_comparer);

            string[] args = splitArgs
                .Select(arg => arg.Trim())
                .Where(arg => !string.IsNullOrEmpty(arg))
                .ToArray();

            foreach (string arg in args)
            {
                if (TryParseNamedArgument(arg, out KeyValuePair<string, string> namedArgument))
                {
                    // Last value wins, matching what most command line applications do with a
                    // repeated option, but every value is kept so nothing the user typed is
                    // lost. ParsedArguments.AllValuesOf exposes them.
                    namedArguments[namedArgument.Key] = namedArgument.Value;

                    if (!allNamedArgumentValues.TryGetValue(namedArgument.Key,
                            out List<string> values))
                    {
                        values = new List<string>();

                        allNamedArgumentValues.Add(namedArgument.Key, values);
                    }

                    values.Add(namedArgument.Value);
                }
                else if (int.TryParse(arg, NumberStyles.Integer,
                             CultureInfo.InvariantCulture, out int intVal))
                {
                    integerArguments.Add(intVal);
                }
                // Deliberately narrower than NumberStyles.Number, which also accepts group
                // separators and a trailing sign. Nobody types "1,234" or "5-" on a command
                // line, and a token wrongly taken for a number is dropped from StringArguments
                // where the caller would look for it.
                else if (decimal.TryParse(arg,
                             NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                             CultureInfo.InvariantCulture, out decimal decimalVal))
                {
                    decimalArguments.Add(decimalVal);
                }
                else
                {
                    stringArguments.Add(arg);
                }
            }

            return new ParsedArguments(
                args,
                integerArguments,
                decimalArguments,
                stringArguments,
                namedArguments,
                allNamedArgumentValues,
                _comparer);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Restores the quoting the shell removed before Main saw the argument. Only whitespace
        /// forces it, because whitespace is what the shell tokenized on. A separator such as
        /// "--" is left alone, since the parser is meant to strip that as a prefix.
        /// </summary>
        private static string Requote(string arg)
        {
            if (arg == null || arg.IndexOf('"') >= 0 || !arg.Any(char.IsWhiteSpace))
            {
                return arg;
            }

            return "\"" + arg + "\"";
        }

        /// <summary>
        /// Splits on the argument separators, except where a separator falls inside a pair of
        /// double quotes, so a value containing a separator survives as one argument. The
        /// quotes group the value and are not part of it, so they are removed as it is read.
        /// </summary>
        private string[] SplitRespectingQuotes(string arguments)
        {
            List<string> splitArgs = new List<string>();
            StringBuilder current = new StringBuilder();
            bool insideQuotes = false;
            int index = 0;

            while (index < arguments.Length)
            {
                if (arguments[index] == '"')
                {
                    insideQuotes = !insideQuotes;
                    index++;

                    continue;
                }

                string separator = insideQuotes ? null : MatchSeparatorAt(arguments, index);

                if (separator == null)
                {
                    current.Append(arguments[index]);
                    index++;

                    continue;
                }

                splitArgs.Add(current.ToString());
                current.Clear();
                index += separator.Length;
            }

            splitArgs.Add(current.ToString());

            // Matches the RemoveEmptyEntries this replaced. A run of separators, or a
            // separator at either end, contributes nothing.
            return splitArgs.Where(a => a.Length > 0).ToArray();
        }

        /// <summary>
        /// Returns the argument separator starting at index, or null if none does. The longest
        /// match wins, so "--" is preferred over "-" however the separators were ordered or
        /// whichever order the fluent builder's HashSet happens to yield them in.
        /// </summary>
        private string MatchSeparatorAt(string arguments, int index)
        {
            string longestMatch = null;

            foreach (string separator in _argSeparators)
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

        private bool TryParseNamedArgument(string argument,
            out KeyValuePair<string, string> namedArgument)
        {
            namedArgument = default;

            // Leftmost separator wins, rather than the first separator in the configured
            // order. Otherwise a value containing a later separator ("--out=C:\build")
            // splits at that one instead of at the separator the caller actually typed.
            int splitIndex = -1;
            string splitSeparator = null;

            foreach (string separator in _keyValueSeparators)
            {
                int separatorIndex = argument.IndexOf(separator, StringComparison.Ordinal);

                if (separatorIndex >= 1 && (splitIndex == -1 || separatorIndex < splitIndex))
                {
                    splitIndex = separatorIndex;
                    splitSeparator = separator;
                }
            }

            if (splitIndex == -1)
            {
                return false;
            }

            namedArgument = new KeyValuePair<string, string>(
                argument.Substring(0, splitIndex),
                argument.Substring(splitIndex + splitSeparator.Length).Trim());

            return true;
        }

        #endregion
    }
}