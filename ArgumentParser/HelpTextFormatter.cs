using System.Text;

namespace ArgumentParser;

/// <summary>
/// Lays out an ArgumentSchema as help text. Kept apart from the schema because formatting
/// and parsing have nothing to do with each other beyond the declarations they share.
/// </summary>
internal static class HelpTextFormatter
{
    private const int Indent = 2;
    private const int GapBeforeDescription = 2;

    // Past this, a long option would squeeze descriptions into a ribbon, so its
    // description moves to the next line instead.
    private const int MaxNameColumn = 30;

    internal static string Format(ArgumentSchema schema, int width)
    {
        if (width < 40)
        {
            width = 40;
        }

        List<string> lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(schema.Description))
        {
            lines.AddRange(Wrap(schema.Description, width));
            lines.Add(string.Empty);
        }

        if (!string.IsNullOrWhiteSpace(schema.Usage))
        {
            lines.Add("Usage: " + schema.Usage);
            lines.Add(string.Empty);
        }

        lines.AddRange(FormatOptions(schema, width));

        return string.Join(Environment.NewLine, TrimTrailingBlanks(lines));
    }

    private static IEnumerable<string> FormatOptions(ArgumentSchema schema, int width)
    {
        if (schema.Options.Count == 0)
        {
            yield break;
        }

        List<string> aliasParts = schema.Options.Select(AliasPart).ToList();
        List<string> nameParts = schema.Options.Select(NamePart).ToList();

        int aliasColumn = aliasParts.Max(part => part.Length);
        int nameColumn = Math.Min(nameParts.Max(part => part.Length), MaxNameColumn);

        int descriptionColumn =
            Indent + aliasColumn + (aliasColumn > 0 ? 1 : 0) + nameColumn
            + GapBeforeDescription;

        for (int index = 0; index < schema.Options.Count; index++)
        {
            string left = new string(' ', Indent)
                + (aliasColumn > 0
                    ? aliasParts[index].PadRight(aliasColumn) + " "
                    : string.Empty)
                + nameParts[index];

            List<string> description =
                Wrap(DescriptionPart(schema.Options[index]), width - descriptionColumn)
                    .ToList();

            if (description.Count == 0)
            {
                yield return left.TrimEnd();

                continue;
            }

            // A name too long for the column gets the line to itself, rather than pushing
            // every other description across to meet it.
            if (left.Length > descriptionColumn - GapBeforeDescription)
            {
                yield return left;
            }
            else
            {
                yield return left.PadRight(descriptionColumn) + description[0];

                description.RemoveAt(0);
            }

            foreach (string continuation in description)
            {
                yield return new string(' ', descriptionColumn) + continuation;
            }
        }
    }

    private static string AliasPart(OptionDefinition option) =>
        option.Aliases.Count == 0 ? string.Empty : string.Join(", ", option.Aliases) + ",";

    private static string NamePart(OptionDefinition option) =>
        option.IsFlag ? option.Name : option.Name + " <" + ValueName(option) + ">";

    private static string ValueName(OptionDefinition option)
    {
        if (option.ValueName != null && option.ValueName.Trim().Length > 0)
        {
            return option.ValueName;
        }

        if (option.ValueType.IsEnum)
        {
            return "name";
        }

        if (option.ValueType == typeof(int) || option.ValueType == typeof(long))
        {
            return "int";
        }

        if (option.ValueType == typeof(decimal) || option.ValueType == typeof(double))
        {
            return "number";
        }

        return "value";
    }

    /// <summary>
    /// The description, followed by the things a reader actually looks for: whether the
    /// option is required, whether it can be repeated, what it defaults to, and for an
    /// enum, what it will accept.
    /// </summary>
    private static string DescriptionPart(OptionDefinition option)
    {
        List<string> notes = new List<string>();

        if (option.IsRequired)
        {
            notes.Add("required");
        }

        if (option.IsRepeatable)
        {
            notes.Add("repeatable");
        }

        if (!option.IsFlag && !option.IsRequired && option.DefaultValue != null)
        {
            notes.Add("default: " + option.DefaultValue);
        }

        // Pipe separated, and last, because the notes are joined with commas and a comma
        // separated list of values inside them reads as more notes.
        if (option.ValueType.IsEnum)
        {
            notes.Add("one of: " + string.Join("|", Enum.GetNames(option.ValueType)));
        }

        string description = option.Description ?? string.Empty;

        if (notes.Count == 0)
        {
            return description;
        }

        string noteText = "(" + string.Join(", ", notes) + ")";

        return description.Length == 0 ? noteText : description + " " + noteText;
    }

    /// <summary>
    /// Wraps at a word boundary, at the width asked for rather than the console's, so
    /// output that is piped to a file looks the same as output that is not.
    /// </summary>
    private static IEnumerable<string> Wrap(string? text, int width)
    {
        // Not string.IsNullOrWhiteSpace: it carries NotNullWhen(false) in the .NET 8
        // reference assembly but not in netstandard2.0's, so that target cannot narrow
        // text afterwards and warns on the loop below.
        if (text == null || text.Trim().Length == 0)
        {
            yield break;
        }

        if (width < 10)
        {
            width = 10;
        }

        StringBuilder line = new StringBuilder();

        foreach (string word in text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > width)
            {
                yield return line.ToString();

                line.Clear();
            }

            if (line.Length > 0)
            {
                line.Append(' ');
            }

            line.Append(word);
        }

        if (line.Length > 0)
        {
            yield return line.ToString();
        }
    }

    private static List<string> TrimTrailingBlanks(List<string> lines)
    {
        while (lines.Count > 0 && lines[lines.Count - 1].Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return lines;
    }
}
