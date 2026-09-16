namespace ArgumentParser;

/// <summary>
/// The default declared for an option, or the absence of one. Exists so that
/// Option&lt;int&gt;("--timeout") can say it was given no default: a plain T parameter
/// would read as 0 for an int, and the help text would then claim a default that was never
/// declared. A T converts to this on its own, so a caller writes "defaultValue: 60" and
/// never sees the type.
/// </summary>
/// <typeparam name="T">The option's value type.</typeparam>
public readonly struct OptionDefault<T>
{
    /// <summary>
    /// True when a default was declared, even one of null.
    /// </summary>
    public bool HasValue { get; }

    /// <summary>
    /// The declared default. Meaningless when HasValue is false.
    /// </summary>
    public T? Value { get; }

    private OptionDefault(T? value)
    {
        HasValue = true;
        Value = value;
    }

    /// <summary>
    /// Wraps a value as a declared default.
    /// </summary>
    public static implicit operator OptionDefault<T>(T? value) => new OptionDefault<T>(value);
}
