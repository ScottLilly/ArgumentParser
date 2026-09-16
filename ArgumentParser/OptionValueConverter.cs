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
    private const NumberStyles IntegerStyles = NumberStyles.Integer;

    private const NumberStyles FractionalStyles =
        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    /// <summary>
    /// True when the type can be the target of a declared option.
    /// </summary>
    internal static bool IsSupported(Type type) =>
        type == typeof(string)
        || type == typeof(bool)
        || type == typeof(int)
        || type == typeof(long)
        || type == typeof(decimal)
        || type == typeof(double)
        || type.IsEnum;

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

        if (targetType == typeof(string))
        {
            return text;
        }

        if (targetType == typeof(bool))
        {
            return bool.TryParse(text, out bool parsedBool) ? (object)parsedBool : null;
        }

        if (targetType == typeof(int))
        {
            return int.TryParse(text, IntegerStyles, CultureInfo.InvariantCulture,
                out int parsedInt)
                ? (object)parsedInt
                : null;
        }

        if (targetType == typeof(long))
        {
            return long.TryParse(text, IntegerStyles, CultureInfo.InvariantCulture,
                out long parsedLong)
                ? (object)parsedLong
                : null;
        }

        if (targetType == typeof(decimal))
        {
            return decimal.TryParse(text, FractionalStyles, CultureInfo.InvariantCulture,
                out decimal parsedDecimal)
                ? (object)parsedDecimal
                : null;
        }

        if (targetType == typeof(double))
        {
            return double.TryParse(text, FractionalStyles, CultureInfo.InvariantCulture,
                out double parsedDouble)
                ? (object)parsedDouble
                : null;
        }

        if (targetType.IsEnum)
        {
            return ConvertEnum(text, targetType, comparer);
        }

        return null;
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
