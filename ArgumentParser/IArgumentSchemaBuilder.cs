using System.Collections.Generic;

namespace ArgumentParser
{
    /// <summary>
    /// Declares the options an application accepts. Start one with ArgumentSchema.Create().
    /// </summary>
    public interface IArgumentSchemaBuilder
    {
        /// <summary>
        /// Declares an option that takes a value.
        /// </summary>
        /// <typeparam name="T">The value's type. string, bool, int, long, decimal, double and any enum are supported.</typeparam>
        /// <param name="name">The option's name as it is typed, including any prefix ("--output").</param>
        /// <param name="alias">(Optional) another name the option answers to, such as "-o".</param>
        /// <param name="required">(Optional) true when leaving the option out is an error.</param>
        /// <param name="repeatable">(Optional) true when the option may be given more than once.</param>
        /// <param name="defaultValue">(Optional) the value used when the option is not given.</param>
        /// <param name="description">(Optional) what the option does, for error messages and help text.</param>
        IArgumentSchemaBuilder Option<T>(string name, string alias = null, bool required = false,
            bool repeatable = false, T defaultValue = default, string description = null);

        /// <summary>
        /// Declares an option that takes a value and answers to several names.
        /// </summary>
        IArgumentSchemaBuilder Option<T>(string name, string[] aliases, bool required = false,
            bool repeatable = false, T defaultValue = default, string description = null);

        /// <summary>
        /// Declares an option that takes no value and is true by its presence alone. Its value
        /// can still be written out in full ("--verbose=false") when a caller wants to.
        /// </summary>
        IArgumentSchemaBuilder Flag(string name, string alias = null, string description = null);

        /// <summary>
        /// Declares a flag that answers to several names.
        /// </summary>
        IArgumentSchemaBuilder Flag(string name, string[] aliases, string description = null);

        /// <summary>
        /// Separators between arguments. Defaults to a single space.
        /// </summary>
        IArgumentSchemaBuilder WithArgumentSeparators(params string[] argumentSeparators);

        /// <summary>
        /// Separators between an option's name and its value when they are written as one
        /// argument. Defaults to ':' and '='.
        /// </summary>
        IArgumentSchemaBuilder WithKeyValueSeparators(params char[] keyValueSeparators);

        /// <summary>
        /// The prefixes that mark an argument as an option name rather than a value, so an
        /// undeclared one can be reported. Defaults to "--" and "-".
        /// </summary>
        IArgumentSchemaBuilder WithOptionPrefixes(params string[] optionPrefixes);

        /// <summary>
        /// Comparer used to match option names. Defaults to StringComparer.OrdinalIgnoreCase.
        /// Pass StringComparer.Ordinal to match names case-sensitively.
        /// </summary>
        IArgumentSchemaBuilder WithComparer(IEqualityComparer<string> comparer);

        /// <summary>
        /// Finishes the declaration and returns the schema to parse with.
        /// </summary>
        ArgumentSchema Build();
    }
}
