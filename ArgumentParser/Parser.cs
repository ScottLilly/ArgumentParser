using System;
using System.Collections.Generic;
using System.Linq;

namespace ArgumentParser
{
    public class Parser
    {
        private static readonly char[] DEFAULT_ARG_SEPARATORS = new[] { ' ' };
        private static readonly char[] DEFAULT_KEY_VALUE_SEPARATORS = new[] { ':', '=' };

        private readonly IEnumerable<char> _argSeparators = new[] { ' ' };
        private readonly IEnumerable<char> _keyValueSeparators = new[] { ':', '=' };

        public Parser() :
            this(DEFAULT_ARG_SEPARATORS, DEFAULT_KEY_VALUE_SEPARATORS)
        {
        }

        public Parser(IEnumerable<char> argSeparators) :
            this(argSeparators, DEFAULT_KEY_VALUE_SEPARATORS)
        {
        }

        public Parser(IEnumerable<char> argSeparators, IEnumerable<char> keyValueSeparators)
        {
            _argSeparators = argSeparators;
            _keyValueSeparators = keyValueSeparators;
        }

        public ParsedArguments Parse(string[] args)
        {
            var integerArguments = new List<int>();
            var decimalArguments = new List<decimal>();
            var stringArguments = new List<string>();
            var namedArguments = new Dictionary<string, string>();

            foreach (var arg in args)
            {
                if (TryParseNamedArgument(arg, out var key, out var value))
                {
                    namedArguments[key] = value;
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
                args.ToList(),
                integerArguments,
                decimalArguments,
                stringArguments,
                namedArguments);
        }

        public ParsedArguments Parse(string arguments)
        {
            return Parse(
                arguments.Split(
                    _argSeparators.ToArray(), 
                    StringSplitOptions.RemoveEmptyEntries));
        }

        private bool TryParseNamedArgument(string argument, out string key, out string value)
        {
            foreach (var separator in _keyValueSeparators)
            {
                if (argument.Contains(separator))
                {
                    var parts = 
                        argument.Split(new[] { separator }, 2, StringSplitOptions.None);

                    if (parts.Length == 2)
                    {
                        key = parts[0];
                        value = parts[1];

                        return true;
                    }
                }
            }

            key = null;
            value = null;

            return false;
        }
    }
}