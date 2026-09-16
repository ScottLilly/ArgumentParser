using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

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

        private readonly string[] _argSeparators;
        private readonly string[] _keyValueSeparators;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the Parser class with default argument and key/value separators.
        /// </summary>
        /// <param name="argSeparators">(Optional) array of characters that indicate a separator between arguments. Default value is { ' ' }</param>
        /// <param name="keyValueSeparators">(Optional) array of charcters that indicate a separator between the key and value in a key/value argument. Default values are { ':', '=' }</param>
        public Parser(char[] argSeparators = null, char[] keyValueSeparators = null)
        {
            _argSeparators =
                argSeparators?.Select(c => c.ToString()).ToArray()
                ?? s_defaultArgSeparators.ToArray();

            _keyValueSeparators =
                keyValueSeparators?.Select(c => c.ToString()).ToArray()
                ?? s_defaultKeyValueSeparators.ToArray();
        }

        /// <summary>
        /// Initializes a new instance of the Parser class with default argument and key/value separators.
        /// </summary>
        /// <param name="argSeparators">(Optional) array of strings that indicate a separator between arguments. Default value is { " " }</param>
        /// <param name="keyValueSeparators">(Optional) array of charcters that indicate a separator between the key and value in a key/value argument. Default values are { ':', '=' }</param>
        public Parser(string[] argSeparators, char[] keyValueSeparators = null)
        {
            _argSeparators =
                argSeparators
                ?? s_defaultArgSeparators.ToArray();

            _keyValueSeparators =
                keyValueSeparators?.Select(c => c.ToString()).ToArray()
                ?? s_defaultKeyValueSeparators.ToArray();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Parses an array of string arguments into a ParsedArguments object.
        /// A null array means no arguments, and returns an empty ParsedArguments.
        /// Null elements within the array are ignored.
        /// </summary>
        /// <param name="args">Array of string arguments to parse. May be null.</param>
        /// <returns>ParsedArguments object, populate with values from arguments parameter</returns>
        public ParsedArguments Parse(string[] args)
        {
            // Concatenate the array of strings into a single string,
            // before passing to the Parse method that accepts a single string parameter.
            // This is to handle command line arguments that may be intended as key/value pairs,
            // but have already been split into an array by a console app's static Main method.
            return Parse(args == null ? string.Empty : string.Join(" ", args));
        }

        /// <summary>
        /// Parses a single string of arguments into a ParsedArguments object.
        /// Numeric arguments are recognized with the invariant culture, so the same command
        /// line classifies identically on every machine. An argument counts as a number only
        /// if it is digits with an optional leading sign and, for decimals, a single period.
        /// Anything else, including group separators and exponents, is a string argument.
        /// A null string means no arguments, and returns an empty ParsedArguments.
        /// </summary>
        /// <param name="arguments">String containing arguments to parse. May be null.</param>
        /// <returns>ParsedArguments object, populate with values from arguments parameter</returns>
        public ParsedArguments Parse(string arguments)
        {
            // Null is treated as no arguments rather than as an error, so a Main(string[] args)
            // caller does not have to guard the call. Empty input already returned an empty
            // result, and this makes null agree with it.
            string[] splitArgs =
                (arguments ?? string.Empty)
                .Split(_argSeparators, StringSplitOptions.RemoveEmptyEntries);

            List<int> integerArguments = new List<int>();
            List<decimal> decimalArguments = new List<decimal>();
            List<string> stringArguments = new List<string>();
            Dictionary<string, string> namedArguments = new Dictionary<string, string>();

            string[] args = splitArgs
                .Select(arg => arg.Trim())
                .Where(arg => !string.IsNullOrEmpty(arg))
                .ToArray();

            foreach (string arg in args)
            {
                if (TryParseNamedArgument(arg, out KeyValuePair<string, string> namedArgument))
                {
                    namedArguments[namedArgument.Key] = namedArgument.Value;
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
                namedArguments);
        }

        #endregion

        #region Private Methods

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