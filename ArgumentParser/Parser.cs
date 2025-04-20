using System;
using System.Collections.Generic;
using System.Linq;

namespace ArgumentParser
{
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

        public Parser(char[] argSeparators = null, char[] keyValueSeparators = null)
        {
            _argSeparators = argSeparators ?? _defaultArgSeparators;
            _keyValueSeparators = keyValueSeparators ?? _defaultKeyValueSeparators;
        }

        public Parser(string[] stringArgSeparators, char[] keyValueSeparators = null)
        {
            _stringArgSeparators = stringArgSeparators ?? _defaultStringArgSeparators;
            _keyValueSeparators = keyValueSeparators ?? _defaultKeyValueSeparators;
        }

        #endregion

        #region Public Methods

        public ParsedArguments Parse(string[] args)
        {
            var integerArguments = new List<int>();
            var decimalArguments = new List<decimal>();
            var stringArguments = new List<string>();
            var namedArguments = new Dictionary<string, string>();
            var flags = new List<string>();

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
                args.ToList(), 
                integerArguments, 
                decimalArguments, 
                stringArguments, 
                namedArguments);
        }

        public ParsedArguments Parse(string arguments)
        {
            string[] splitArgs;

            if (_stringArgSeparators != null)
            {
                splitArgs = SplitByStringSeparators(arguments, _stringArgSeparators);
            }
            else
            {
                splitArgs = arguments.Split(_argSeparators, StringSplitOptions.RemoveEmptyEntries);
            }

            return Parse(splitArgs);
        }

        #endregion

        #region Private Methods

        private bool TryParseNamedArgument(string argument, out KeyValuePair<string, string> namedArgument)
        {
            int separatorIndex = argument.IndexOfAny(_keyValueSeparators);

            if (separatorIndex >= 0)
            {
                string key = argument.Substring(0, separatorIndex);
                string value = argument.Substring(separatorIndex + 1);

                namedArgument = new KeyValuePair<string, string>(key, value);

                return true;
            }

            return false;
        }

        private string[] SplitByStringSeparators(string input, string[] separators)
        {
            return input.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        }

        #endregion
    }
}