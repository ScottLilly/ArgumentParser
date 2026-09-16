using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ArgumentParser
{
    /// <summary>
    /// One declared option: what it is called, what it accepts, and what it means. This is the
    /// piece the untyped Parser has no equivalent of, and what makes unknown options, type
    /// conversion, required checks and repeated values detectable.
    /// </summary>
    public class OptionDefinition
    {
        /// <summary>
        /// The option's full name, including any prefix, as it is typed on the command line.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Other names the option answers to, such as a single character short form.
        /// </summary>
        public IReadOnlyList<string> Aliases { get; }

        /// <summary>
        /// The type values are converted to. Always bool for a flag.
        /// </summary>
        public Type ValueType { get; }

        /// <summary>
        /// True when the option takes no value and is true by its presence alone.
        /// </summary>
        public bool IsFlag { get; }

        /// <summary>
        /// True when leaving the option out is an error.
        /// </summary>
        public bool IsRequired { get; }

        /// <summary>
        /// True when the option may be given more than once, collecting every value.
        /// </summary>
        public bool IsRepeatable { get; }

        /// <summary>
        /// The value used when the option is not given. Null when none was declared.
        /// </summary>
        public object? DefaultValue { get; }

        /// <summary>
        /// What the option does, for help text and error messages.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// What the option's value is called in help text, such as "path" in "--output path".
        /// Falls back to something derived from the declared type when none was given.
        /// </summary>
        public string? ValueName { get; }

        internal OptionDefinition(string name, IEnumerable<string>? aliases, Type valueType,
            bool isFlag, bool isRequired, bool isRepeatable, object? defaultValue,
            string? description, string? valueName)
        {
            ValueName = valueName;

            Name = name;
            Aliases = new ReadOnlyCollection<string>((aliases ?? Enumerable.Empty<string>())
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .ToList());
            ValueType = valueType;
            IsFlag = isFlag;
            IsRequired = isRequired;
            IsRepeatable = isRepeatable;
            DefaultValue = defaultValue;
            Description = description;
        }

        /// <summary>
        /// Every name this option answers to, its own first.
        /// </summary>
        internal IEnumerable<string> AllNames()
        {
            yield return Name;

            foreach (string alias in Aliases)
            {
                yield return alias;
            }
        }
    }
}
