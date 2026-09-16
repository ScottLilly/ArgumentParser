namespace ArgumentParser;

/// <summary>
/// The kinds of problem a schema parse can report. Every one of them is invisible without
/// a declaration, which is why error reporting arrived with the schema rather than before it.
/// </summary>
public enum ParseErrorKind
{
    /// <summary>
    /// An option-shaped argument that was not declared, such as a misspelled option name.
    /// </summary>
    UnknownOption,

    /// <summary>
    /// A declared option that takes a value had none: it was the last argument, or the
    /// argument after it was another option.
    /// </summary>
    MissingValue,

    /// <summary>
    /// A value that could not be converted to the type the option was declared with.
    /// </summary>
    UnconvertibleValue,

    /// <summary>
    /// An option declared as required that did not appear.
    /// </summary>
    MissingRequiredOption,

    /// <summary>
    /// An option given more than once that was not declared repeatable.
    /// </summary>
    OptionNotRepeatable
}
