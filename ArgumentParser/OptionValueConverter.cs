using System.Globalization;

namespace ArgumentParser;

/// <summary>
/// Converts a raw argument value to the type its option was declared with. The number
/// rules deliberately match the untyped Parser's: invariant culture, an optional leading
/// sign, and for the fractional types a single period. A command line means the same thing
/// on a developer machine and on a CI agent.
/// </summary>
internal static class OptionValueConverter
{
    private const NumberStyles INTEGER_STYLES = NumberStyles.Integer;

    private const NumberStyles FRACTIONAL_STYLES =
        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    /// <summary>
    /// Every type an option can be declared with, and how each one reads its text. This is
    /// the single answer to both "is that type allowed" and "what does that text mean", so
    /// the two cannot disagree and a new type is added in one place.
    /// <para>
    /// An enum is the one supported type not in here. There is no fixed set of enum types
    /// to key by, and converting one needs the schema's comparer, which a table keyed only
    /// by type has nowhere to put.
    /// </para>
    /// </summary>
    private static readonly Dictionary<Type, Func<string, object?>> s_converters =
        new Dictionary<Type, Func<string, object?>>
        {
            [typeof(string)] = text => text,
            [typeof(bool)] = text =>
                bool.TryParse(text, out bool value) ? (object)value : null,
            [typeof(int)] = text =>
                int.TryParse(text, INTEGER_STYLES, CultureInfo.InvariantCulture,
                    out int value)
                    ? (object)value
                    : null,
            [typeof(long)] = text =>
                long.TryParse(text, INTEGER_STYLES, CultureInfo.InvariantCulture,
                    out long value)
                    ? (object)value
                    : null,
            [typeof(decimal)] = text =>
                decimal.TryParse(text, FRACTIONAL_STYLES, CultureInfo.InvariantCulture,
                    out decimal value)
                    ? (object)value
                    : null,
            [typeof(double)] = text =>
                double.TryParse(text, FRACTIONAL_STYLES, CultureInfo.InvariantCulture,
                    out double value)
                    ? (object)value
                    : null
        };

    /// <summary>
    /// True when the type can be the target of a declared option.
    /// </summary>
    internal static bool IsSupported(Type type) =>
        s_converters.ContainsKey(type) || type.IsEnum;

    /// <summary>
    /// The converted value, or null when the text cannot be read as the target type. None
    /// of the supported types converts to null, so null is unambiguous as "no".
    /// </summary>
    /// <param name="text">The raw value, as it was typed.</param>
    /// <param name="targetType">The type the option was declared with.</param>
    /// <param name="comparer">The schema's comparer, which matches an enum value's name the same way it matches an option's name.</param>
    internal static object? Convert(string? text, Type targetType,
        IEqualityComparer<string> comparer)
    {
        if (text == null)
        {
            return null;
        }

        if (targetType.IsEnum)
        {
            return ConvertEnum(text, targetType, comparer);
        }

        return s_converters.TryGetValue(targetType, out Func<string, object?>? converter)
            ? converter(text)
            : null;
    }

    /// <summary>
    /// Matches a declared name with the schema's comparer, so a schema that is strict about
    /// the case of its option names is strict about the case of its enum values too. Names
    /// only, deliberately: Enum.TryParse also accepts the numeric form, so "3" would match
    /// any enum at all, and it accepts a comma separated list, so "Sales,Marketing" would
    /// silently combine two values.
    /// </summary>
    private static object? ConvertEnum(string text, Type enumType,
        IEqualityComparer<string> comparer)
    {
        foreach (string name in Enum.GetNames(enumType))
        {
            if (comparer.Equals(name, text))
            {
                return Enum.Parse(enumType, name);
            }
        }

        return null;
    }
}
