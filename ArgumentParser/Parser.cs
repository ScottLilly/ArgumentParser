using System;
using System.Collections.Generic;
using System.Linq;

namespace ArgumentParser
{
    public class Parser
    {
        private readonly string _arguments;
        private readonly char[] _separators;

        public IEnumerable<string> Arguments => 
            _arguments
                .Split(_separators, StringSplitOptions.RemoveEmptyEntries);

        private IEnumerable<string> IntegerArgumentsAsStrings =>
            Arguments
                .Where(a => int.TryParse(a, out _));
        public IEnumerable<int> IntegerArguments =>
            IntegerArgumentsAsStrings
                .Select(int.Parse);

        private IEnumerable<string> DecimalArgumentsAsStrings =>
            Arguments
                .Except(IntegerArgumentsAsStrings)
                .Where(a => decimal.TryParse(a, out _));
        public IEnumerable<decimal> DecimalArguments =>
            DecimalArgumentsAsStrings
                .Select(decimal.Parse);

        public IEnumerable<string> StringArguments =>
            Arguments
                .Except(IntegerArgumentsAsStrings)
                .Except(DecimalArgumentsAsStrings);

        public Parser(string arguments) 
            : this(arguments, new[]{' '})
        {
        }

        public Parser(string arguments, char[] separators)
        {
            _arguments = arguments;
            _separators = separators;
        }
    }
}