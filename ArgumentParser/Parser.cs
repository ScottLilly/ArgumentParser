using System;
using System.Linq;

namespace ArgumentParser
{
    public class Parser
    {
        private readonly char[] _argSeparators;
        private readonly char[] _keyValueSeparators;

        public Parser() : 
            this(new[] { ' ' }, new[] { ':' })
        {
        }

        public Parser(char[] argSeparators) :
            this(argSeparators, new[] { ':' })
        {
        }

        public Parser(char[] argSeparators, char[] keyValueSeparators)
        {
            _argSeparators = argSeparators;
            _keyValueSeparators = keyValueSeparators;
        }

        public ParsedArguments Parse(string arguments)
        {
            // Initial split of arguments
            var splitArguments =
                arguments.Split(_argSeparators,
                        StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

            // Find arguments that are integers or decimals
            var integerArgumentsAsStrings =
                splitArguments
                    .Where(a => int.TryParse(a, out _))
                    .ToList();

            var decimalArgumentsAsStrings =
                splitArguments
                    .Except(integerArgumentsAsStrings)
                    .Where(a => decimal.TryParse(a, out _))
                    .ToList();

            // Variables for arguments by datatype
            var integerArguments =
                integerArgumentsAsStrings
                    .Select(int.Parse)
                    .ToList();

            var decimalArguments =
                decimalArgumentsAsStrings
                    .Select(decimal.Parse)
                    .ToList();

            var stringArguments =
                splitArguments
                    .Where(a => !a.Contains(_keyValueSeparators))
                    .Except(integerArgumentsAsStrings)
                    .Except(decimalArgumentsAsStrings)
                    .ToList();

            var namedArguments =
                splitArguments
                    .Where(a => a.Contains(_keyValueSeparators[0]))
                    .Select(a => a.Split(_keyValueSeparators, 2))
                    .Where(a => a.Length == 2)
                    .ToDictionary(a => a[0], a => a[1]);

            return new 
                ParsedArguments(
                    splitArguments,
                    integerArguments,
                    decimalArguments, 
                    stringArguments,
                    namedArguments);
        }
    }
}