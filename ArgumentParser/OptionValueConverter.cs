using System;
using System.Globalization;

namespace ArgumentParser
{
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

        internal static bool TryConvert(string text, Type targetType, out object value)
        {
            value = null;

            if (text == null)
            {
                return false;
            }

            if (targetType == typeof(string))
            {
                value = text;

                return true;
            }

            if (targetType == typeof(bool))
            {
                if (!bool.TryParse(text, out bool parsedBool))
                {
                    return false;
                }

                value = parsedBool;

                return true;
            }

            if (targetType == typeof(int))
            {
                if (!int.TryParse(text, IntegerStyles, CultureInfo.InvariantCulture,
                        out int parsedInt))
                {
                    return false;
                }

                value = parsedInt;

                return true;
            }

            if (targetType == typeof(long))
            {
                if (!long.TryParse(text, IntegerStyles, CultureInfo.InvariantCulture,
                        out long parsedLong))
                {
                    return false;
                }

                value = parsedLong;

                return true;
            }

            if (targetType == typeof(decimal))
            {
                if (!decimal.TryParse(text, FractionalStyles, CultureInfo.InvariantCulture,
                        out decimal parsedDecimal))
                {
                    return false;
                }

                value = parsedDecimal;

                return true;
            }

            if (targetType == typeof(double))
            {
                if (!double.TryParse(text, FractionalStyles, CultureInfo.InvariantCulture,
                        out double parsedDouble))
                {
                    return false;
                }

                value = parsedDouble;

                return true;
            }

            if (targetType.IsEnum)
            {
                return TryConvertEnum(text, targetType, out value);
            }

            return false;
        }

        /// <summary>
        /// Matches a declared name, ignoring case. Names only, deliberately: Enum.TryParse also
        /// accepts the numeric form, so "3" would match any enum at all, and it accepts a comma
        /// separated list, so "Sales,Marketing" would silently combine two values.
        /// </summary>
        private static bool TryConvertEnum(string text, Type enumType, out object value)
        {
            value = null;

            foreach (string name in Enum.GetNames(enumType))
            {
                if (string.Equals(name, text, StringComparison.OrdinalIgnoreCase))
                {
                    value = Enum.Parse(enumType, name);

                    return true;
                }
            }

            return false;
        }
    }
}
