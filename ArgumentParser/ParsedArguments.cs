using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ArgumentParser
{
    /// <summary>
    /// The parsed arguments from string, or string aray, input.
    /// </summary>
    public class ParsedArguments
    {
        /// <summary>
        /// All parsed arguments
        /// </summary>
        public IReadOnlyList<string> Arguments { get; }

        /// <summary>
        /// Parsed integer arguments
        /// </summary>
        public IReadOnlyList<int> IntegerArguments { get; }

        /// <summary>
        /// Parsed decimal arguments
        /// </summary>
        public IReadOnlyList<decimal> DecimalArguments { get; }

        /// <summary>
        /// Parsed string arguments, not including named (key/value) arguments
        /// </summary>
        public IReadOnlyList<string> StringArguments { get; }

        /// <summary>
        /// Parsed named arguments, where the key is the argument name and the value is the argument value
        /// </summary>
        public IReadOnlyDictionary<string, string> NamedArguments { get; }

        /// <summary>
        /// Returns all enum arguments of the specified type, parsed from the string arguments.
        /// This only checks string arguments, not named arguments or integer/decimal arguments,
        /// to prevent confusion with numeric values that might match enum option integer values.
        /// </summary>
        /// <typeparam name="T">Enum to check options against string arguments</typeparam>
        /// <returns>IEnumerable of string parameters that match a value of the enum</returns>
        public IEnumerable<T> EnumArgumentsOfType<T>() where T : struct =>
            StringArguments.Where(a => Enum.TryParse(a, true, out T _))
                .Select(a => (T)Enum.Parse(typeof(T), a, true));

        internal ParsedArguments(
            IEnumerable<string> arguments,
            IEnumerable<int> integerArguments,
            IEnumerable<decimal> decimalArguments,
            IEnumerable<string> stringArguments,
            IDictionary<string, string> namedArguments)
        {
            Arguments = new ReadOnlyCollection<string>(arguments.ToList());
            IntegerArguments = new ReadOnlyCollection<int>(integerArguments.ToList());
            DecimalArguments = new ReadOnlyCollection<decimal>(decimalArguments.ToList());
            StringArguments = new ReadOnlyCollection<string>(stringArguments.ToList());
            NamedArguments = new ReadOnlyDictionary<string, string>(namedArguments);
        }
    }
}