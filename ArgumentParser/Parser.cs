using System;
using System.Collections.Generic;
using System.Linq;

namespace ArgumentParser
{
    public class Parser
    {
        private static readonly char[] _defaultArgSeparators = { ' ' };
        private static readonly char[] _defaultKeyValueSeparators = { ':', '=' };

        private readonly char[] _argSeparators;
        private readonly char[] _keyValueSeparators;

        public Parser(char[] argSeparators = null, char[] keyValueSeparators = null)
        {
            _argSeparators = argSeparators ?? _defaultArgSeparators;
            _keyValueSeparators = keyValueSeparators ?? _defaultKeyValueSeparators;
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
            return Parse(arguments.Split(_argSeparators, StringSplitOptions.RemoveEmptyEntries));
        }

        private bool TryParseNamedArgument(string argument, out string key, out string value)
        {
            var separatorIndex = argument.IndexOfAny(_keyValueSeparators);

            if (separatorIndex >= 0)
            {
                key = argument.Substring(0, separatorIndex);
                value = argument.Substring(separatorIndex + 1);

                return true;
            }

            key = null;
            value = null;

            return false;
        }
    }
}