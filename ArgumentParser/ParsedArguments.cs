using System.Collections.ObjectModel;

namespace ArgumentParser;

/// <summary>
/// The parsed arguments from string, or string aray, input.
/// </summary>
public class ParsedArguments
{
    private static readonly IReadOnlyList<string> s_noValues =
        new ReadOnlyCollection<string>(new string[0]);

    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _allNamedArgumentValues;

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
    /// Parsed named arguments, where the key is the argument name and the value is the argument value.
    /// A name given more than once holds the last value, which is what most command line
    /// applications do with a repeated option. Use AllValuesOf to reach the earlier values.
    /// Names are matched with the parser's comparer, which ignores case unless the caller
    /// asked for something else, so "--output" and "--Output" are the same argument.
    /// </summary>
    public IReadOnlyDictionary<string, string> NamedArguments { get; }

    /// <summary>
    /// Returns every value given for a named argument, in the order they appeared, so a
    /// name repeated on the command line ("--exclude a --exclude b") loses nothing.
    /// NamedArguments holds the last of these values, and the two always agree on which
    /// names are present.
    /// </summary>
    /// <param name="key">Name of the argument, including any prefix the parser did not strip. May be null.</param>
    /// <returns>
    /// Every value given for the name. An empty list if the name was not given, or if key is null.
    /// </returns>
    public IReadOnlyList<string> AllValuesOf(string? key)
    {
        if (key == null
            || !_allNamedArgumentValues.TryGetValue(key, out IReadOnlyList<string>? values))
        {
            return s_noValues;
        }

        return values;
    }

    /// <summary>
    /// Returns all enum arguments of the specified type, parsed from the string arguments.
    /// This only checks string arguments, not named arguments or integer/decimal arguments,
    /// to prevent confusion with numeric values that might match enum option integer values.
    /// </summary>
    /// <typeparam name="T">Enum to check options against string arguments</typeparam>
    /// <returns>IEnumerable of string parameters that match a value of the enum</returns>
    public IEnumerable<T> EnumArgumentsOfType<T>() where T : struct, Enum
    {
        foreach (string argument in StringArguments)
        {
            if (Enum.TryParse(argument, true, out T value))
            {
                yield return value;
            }
        }
    }

    internal ParsedArguments(
        IEnumerable<string> arguments,
        IEnumerable<int> integerArguments,
        IEnumerable<decimal> decimalArguments,
        IEnumerable<string> stringArguments,
        IDictionary<string, string> namedArguments,
        IDictionary<string, List<string>> allNamedArgumentValues,
        IEqualityComparer<string> comparer)
    {
        Arguments = new ReadOnlyCollection<string>(arguments.ToList());
        IntegerArguments = new ReadOnlyCollection<int>(integerArguments.ToList());
        DecimalArguments = new ReadOnlyCollection<decimal>(decimalArguments.ToList());
        StringArguments = new ReadOnlyCollection<string>(stringArguments.ToList());
        NamedArguments = new ReadOnlyDictionary<string, string>(namedArguments);

        _allNamedArgumentValues =
            new ReadOnlyDictionary<string, IReadOnlyList<string>>(
                allNamedArgumentValues.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (IReadOnlyList<string>)new ReadOnlyCollection<string>(kvp.Value),
                    comparer));
    }
}
