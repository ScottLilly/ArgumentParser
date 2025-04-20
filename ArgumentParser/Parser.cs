using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        /// </summary>
        /// <param name="args">Array of string arguments to parse</param>
        /// <returns>ParsedArguments object, populate with values from arguments parameter</returns>
        public ParsedArguments Parse(string[] args)
        {
            // Concatenate the array of strings into a single string,
            // before passing to the Parse method that accepts a single string parameter.
            // This is to handle command line arguments that may be intended as key/value pairs,
            // but have already been split into an array by a console app's static Main method.
            return Parse(string.Join(" ", args));
        }

        /// <summary>
        /// Parses a single string of arguments into a ParsedArguments object.
        /// </summary>
        /// <param name="arguments">String containing arguments to parse</param>
        /// <returns>ParsedArguments object, populate with values from arguments parameter</returns>
        public ParsedArguments Parse(string arguments)
        {
            string[] splitArgs = 
                arguments.Split(_argSeparators, StringSplitOptions.RemoveEmptyEntries);

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
                else if (int.TryParse(arg, out int intVal))
                {
                    integerArguments.Add(intVal);
                }
                else if (decimal.TryParse(arg, out decimal decimalVal))
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

            foreach (string separator in _keyValueSeparators)
            {
                int separatorIndex = argument.IndexOf(separator, StringComparison.Ordinal);

                if (separatorIndex >= 1)
                {
                    namedArgument = new KeyValuePair<string, string>(
                        argument.Substring(0, separatorIndex),
                        argument.Substring(separatorIndex + separator.Length).Trim());

                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}