using System.Collections.ObjectModel;

namespace ArgumentParser;

/// <summary>
/// The outcome of parsing against a schema: whether it worked, what was wrong if it did
/// not, and the converted values. A result object rather than an exception, because a
/// command line application usually wants to print every problem at once.
/// </summary>
public sealed class SchemaParseResult
{
    private readonly ArgumentSchema _schema;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<object>> _values;

    /// <summary>
    /// True when nothing was wrong with the arguments. Check this before reading values.
    /// </summary>
    public bool Success => Errors.Count == 0;

    /// <summary>
    /// Every problem found, in the order they were found. Empty when Success is true.
    /// </summary>
    public IReadOnlyList<ParseError> Errors { get; }

    /// <summary>
    /// Arguments that were not option-shaped and so were not matched to a declaration,
    /// in the order they appeared. Positional arguments are not declared, so they are
    /// handed back as typed and never reported as unknown.
    /// </summary>
    public IReadOnlyList<string> PositionalArguments { get; }

    /// <summary>
    /// True when "--help" was asked for. Nothing else is reported when it is, since a
    /// request to see the options is not the moment to complain that one is missing.
    /// Print ArgumentSchema.HelpText() and stop.
    /// </summary>
    public bool HelpRequested { get; }

    internal SchemaParseResult(ArgumentSchema schema,
        IDictionary<string, IReadOnlyList<object>> values,
        IEnumerable<string> positionalArguments,
        IEnumerable<ParseError> errors,
        IEqualityComparer<string> comparer,
        bool helpRequested)
    {
        HelpRequested = helpRequested;

        _schema = schema;
        _values = new ReadOnlyDictionary<string, IReadOnlyList<object>>(
            values.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, comparer));

        PositionalArguments =
            new ReadOnlyCollection<string>(positionalArguments.ToList());
        Errors = new ReadOnlyCollection<ParseError>(errors.ToList());
    }

    /// <summary>
    /// True when the option appeared in the arguments, whatever its value. This is how to
    /// read a flag, and how to tell "not given" from "given the same value as the default".
    /// </summary>
    /// <param name="name">The option's name or any of its aliases.</param>
    /// <exception cref="ArgumentException">The name was never declared.</exception>
    public bool IsSet(string name) =>
        _values.ContainsKey(RequireDeclared(name).Name);

    /// <summary>
    /// The option's value, converted to the type it was declared with. Falls back to the
    /// declared default when the option was not given. An option given more than once
    /// returns its last value, matching what NamedArguments does on the untyped Parser.
    /// </summary>
    /// <typeparam name="T">The type the option was declared with.</typeparam>
    /// <param name="name">The option's name or any of its aliases.</param>
    /// <exception cref="ArgumentException">The name was never declared.</exception>
    /// <exception cref="InvalidOperationException">T is not the declared type.</exception>
    public T? ValueOf<T>(string name)
    {
        OptionDefinition option = RequireDeclared(name);

        RequireDeclaredType<T>(option);

        if (!_values.TryGetValue(option.Name, out IReadOnlyList<object>? values)
            || values.Count == 0)
        {
            // A reference-typed option with no declared default really is null here, so
            // the return type says so rather than pretending otherwise.
            return option.DefaultValue == null ? default : (T)option.DefaultValue;
        }

        return (T)values[values.Count - 1];
    }

    /// <summary>
    /// Every value given for the option, in the order given, converted to the type it was
    /// declared with. Empty when the option was not given. This is what a repeatable option
    /// is for ("--exclude bin --exclude obj").
    /// </summary>
    /// <typeparam name="T">The type the option was declared with.</typeparam>
    /// <param name="name">The option's name or any of its aliases.</param>
    /// <exception cref="ArgumentException">The name was never declared.</exception>
    /// <exception cref="InvalidOperationException">T is not the declared type.</exception>
    public IReadOnlyList<T> AllValuesOf<T>(string name)
    {
        OptionDefinition option = RequireDeclared(name);

        RequireDeclaredType<T>(option);

        if (!_values.TryGetValue(option.Name, out IReadOnlyList<object>? values))
        {
            return new ReadOnlyCollection<T>(new T[0]);
        }

        return new ReadOnlyCollection<T>(values.Cast<T>().ToList());
    }

    /// <summary>
    /// Every error message, one per line, ready to print. Empty when Success is true.
    /// </summary>
    public string ErrorText() =>
        Errors.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, Errors.Select(e => e.Message));

    private OptionDefinition RequireDeclared(string name)
    {
        OptionDefinition? option = _schema.FindOption(name);

        if (option == null)
        {
            // Asking for an option that was never declared is a mistake in the calling
            // code rather than bad input, so it throws instead of returning a default.
            throw new ArgumentException(
                $"'{name}' was not declared on this schema.", nameof(name));
        }

        return option;
    }

    private static void RequireDeclaredType<T>(OptionDefinition option)
    {
        if (option.ValueType != typeof(T))
        {
            throw new InvalidOperationException(
                $"'{option.Name}' was declared as {option.ValueType.Name}, not {typeof(T).Name}.");
        }
    }
}
