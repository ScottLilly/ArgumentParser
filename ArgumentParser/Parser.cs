using System;
using System.Collections.Generic;
using System.Linq;

namespace ArgumentParser
{
    public class Parser
    {
        public List<string> Arguments { get; }

        public List<int> IntegerArguments { get; }
        public List<decimal> DecimalArguments { get; }
        public List<string> StringArguments { get; }

        public Parser(string arguments) 
            : this(arguments, new[]{' '})
        {
        }

        public Parser(string arguments, char[] separators)
        {
            Arguments = 
                arguments.Split(separators, 
                    StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

            IEnumerable<string> integerArgumentsAsStrings = 
                Arguments
                    .Where(a => int.TryParse(a, out _))
                    .ToList();

            IntegerArguments =
                integerArgumentsAsStrings
                    .Select(int.Parse)
                    .ToList();

            IEnumerable<string> decimalArgumentsAsStrings = 
                Arguments
                    .Except(integerArgumentsAsStrings)
                    .Where(a => decimal.TryParse(a, out _))
                    .ToList();

            DecimalArguments =
                decimalArgumentsAsStrings
                    .Select(decimal.Parse)
                    .ToList();

            StringArguments =
                Arguments
                    .Except(integerArgumentsAsStrings)
                    .Except(decimalArgumentsAsStrings)
                    .ToList();
        }
    }
}