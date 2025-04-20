using System;
using System.Collections.Generic;

namespace ArgumentParser
{
    /// <summary>
    /// Parser class for parsing command line arguments into a structured format.
    /// </summary>
    public class Parser
    {
        #region Private variables

        private static readonly char[] _defaultArgSeparators = { ' ' };
        private static readonly char[] _defaultKeyValueSeparators = { ':', '=' };
        private static readonly string[] _defaultStringArgSeparators = { " " };
        private readonly string[] _stringArgSeparators;

        private readonly char[] _argSeparators;
        private readonly char[] _keyValueSeparators;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the Parser class with default argument and key/value separators.
        /// </summary>
        /// <param name="argSeparators">(Optional) array of characters that indicate a separator between arguments. Default value is { ' ' }</param>
        /// <param name="keyValueSeparators">(Optional) array of charcters that indicate a separator between the key and value in a key/value argument. Default values are { ':', '=' }</param>
        public Parser(char[] argSeparators = null, char[] keyValueSeparators = null)
        {
            _argSeparators = argSeparators ?? _defaultArgSeparators;
            _keyValueSeparators = keyValueSeparators ?? _defaultKeyValueSeparators;
        }

        /// <summary>
        /// Initializes a new instance of the Parser class with default argument and key/value separators.
        /// </summary>
        /// <param name="argSeparators">(Optional) array of strings that indicate a separator between arguments. Default value is { " " }</param>
        /// <param name="keyValueSeparators">(Optional) array of charcters that indicate a separator between the key and value in a key/value argument. Default values are { ':', '=' }</param>
        public Parser(string[] stringArgSeparators, char[] keyValueSeparators = null)
        {
            _stringArgSeparators = stringArgSeparators ?? _defaultStringArgSeparators;
            _keyValueSeparators = keyValueSeparators ?? _defaultKeyValueSeparators;
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
            var integerArguments = new List<int>();
            var decimalArguments = new List<decimal>();
            var stringArguments = new List<string>();
            var namedArguments = new Dictionary<string, string>();

            foreach (var arg in args)
            {
                if (TryParseNamedArgument(arg, out KeyValuePair<string, string> namedArgument))
                {
                    namedArguments[namedArgument.Key] = namedArgument.Value;
                }
                else if (int.TryParse(arg, out var intVal))
                {
                    integerArguments.Add(intVal);
                }
                else if (decimal.TryParse(arg, out var decimalVal))
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

        /// <summary>
        /// Parses a single string of arguments into a ParsedArguments object.
        /// </summary>
        /// <param name="arguments">String containing arguments to parse</param>
        /// <returns>ParsedArguments object, populate with values from arguments parameter</returns>
        public ParsedArguments Parse(string arguments)
        {
            // Split the string into an array of arguments.
            string[] splitArgs = _stringArgSeparators != null
                ? arguments.Split(_stringArgSeparators, StringSplitOptions.RemoveEmptyEntries)
                : arguments.Split(_argSeparators, StringSplitOptions.RemoveEmptyEntries);

            return Parse(splitArgs);
        }

        #endregion

        #region Private Methods

        private bool TryParseNamedArgument(string argument, 
            out KeyValuePair<string, string> namedArgument)
        {
            int separatorIndex = argument.IndexOfAny(_keyValueSeparators);

            // No separator found, or was found in the first position,
            // so this is not a named argument.
            if (separatorIndex < 1)
            {
                return false;
            }

            // Split the argument into key and value parts.
            namedArgument =
                new KeyValuePair<string, string>(
                    argument.Substring(0, separatorIndex),
                    argument.Substring(separatorIndex + 1));

            return true;
        }

        #endregion
    }
}