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

        // By only trying to parse the StringArguments to the enum,
        // this will ignore integer parameters which could map to an enum value.
        public IEnumerable<T> EnumArgumentsOfType<T>() where T : struct =>
            StringArguments.Where(a => Enum.TryParse(a, true, out T _))
                .Select(a => (T)Enum.Parse(typeof(T), a, true));

        public Parser(string arguments) 
            : this(arguments, new[]{' '})
        {
        }

        public Parser(string arguments, char[] separators)
        {
            // Initial split of arguments
            Arguments = 
                arguments.Split(separators, 
                    StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

            // Find arguments that are integers or decimals
            var integerArgumentsAsStrings = 
                Arguments
                    .Where(a => int.TryParse(a, out _))
                    .ToList();

            var decimalArgumentsAsStrings =
                Arguments
                    .Except(integerArgumentsAsStrings)
                    .Where(a => decimal.TryParse(a, out _))
                    .ToList();

            // Populate properties with arguments by datatype
            IntegerArguments =
                integerArgumentsAsStrings
                    .Select(int.Parse)
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