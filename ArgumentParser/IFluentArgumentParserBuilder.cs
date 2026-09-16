namespace ArgumentParser;

/// <summary>
/// Collects the separators a Parser should use, then parses with them. Start one with
/// FluentArgumentParser.Create. Every method returns the builder, so calls chain.
/// <para>
/// A separator set that is never added to falls back to the Parser default, so
/// Create().Parse(...) behaves the same as new Parser().Parse(...).
/// </para>
/// </summary>
public interface IFluentArgumentParserBuilder
{
    // Argument separators can be added as single characters or strings,
    // to allow for multi-character separators if needed.

    /// <summary>
    /// Adds a character that separates one argument from the next. Defaults to a space
    /// when none is added.
    /// </summary>
    /// <param name="argumentSeparator">The character to split arguments on.</param>
    /// <returns>The same builder, so calls chain.</returns>
    IFluentArgumentParserBuilder AddArgumentSeparator(char argumentSeparator);

    /// <summary>
    /// Adds several characters that separate one argument from the next.
    /// </summary>
    /// <param name="argumentSeparators">The characters to split arguments on.</param>
    /// <returns>The same builder, so calls chain.</returns>
    IFluentArgumentParserBuilder AddArgumentSeparators(char[] argumentSeparators);

    /// <summary>
    /// Adds a string that separates one argument from the next, for a separator of more
    /// than one character such as "--". Where two separators match at the same position
    /// the longest wins, so "--" is preferred over "-".
    /// </summary>
    /// <param name="argumentSeparator">The string to split arguments on.</param>
    /// <returns>The same builder, so calls chain.</returns>
    IFluentArgumentParserBuilder AddArgumentSeparator(string argumentSeparator);

    /// <summary>
    /// Adds several strings that separate one argument from the next.
    /// </summary>
    /// <param name="argumentSeparator">The strings to split arguments on.</param>
    /// <returns>The same builder, so calls chain.</returns>
    IFluentArgumentParserBuilder AddArgumentSeparators(string[] argumentSeparator);

    // Key/Value separators can only be added as single characters

    /// <summary>
    /// Adds a character that separates a named argument's name from its value. Defaults
    /// to ':' and '=' when none is added. An argument splits at the leftmost separator
    /// present, so a value may contain any of the others.
    /// </summary>
    /// <param name="keyValueSeparator">The character to split a name from its value on.</param>
    /// <returns>The same builder, so calls chain.</returns>
    IFluentArgumentParserBuilder AddKeyValueSeparator(char keyValueSeparator);

    /// <summary>
    /// Adds several characters that separate a named argument's name from its value.
    /// </summary>
    /// <param name="keyValueSeparators">The characters to split a name from its value on.</param>
    /// <returns>The same builder, so calls chain.</returns>
    IFluentArgumentParserBuilder AddKeyValueSeparators(char[] keyValueSeparators);

    /// <summary>
    /// Sets the comparer used to match named argument names. Defaults to
    /// StringComparer.OrdinalIgnoreCase, so "--output" and "--Output" are the same
    /// argument. Pass StringComparer.Ordinal to match names case-sensitively.
    /// </summary>
    /// <param name="comparer">The comparer to match names with, or null for the default.</param>
    /// <returns>The same builder, so calls chain.</returns>
    IFluentArgumentParserBuilder WithComparer(IEqualityComparer<string>? comparer);

    /// <summary>
    /// Parses a command line with the separators collected so far. A null string means no
    /// arguments, and returns an empty ParsedArguments.
    /// </summary>
    /// <param name="arguments">The arguments to parse. May be null.</param>
    /// <returns>The parsed arguments, sorted into their types.</returns>
    ParsedArguments Parse(string? arguments);

    /// <summary>
    /// Parses an array of arguments, such as the one handed to Main, with the separators
    /// collected so far. A null array means no arguments, and null elements are ignored.
    /// </summary>
    /// <param name="arguments">The arguments to parse. May be null.</param>
    /// <returns>The parsed arguments, sorted into their types.</returns>
    ParsedArguments Parse(string?[]? arguments);
}
