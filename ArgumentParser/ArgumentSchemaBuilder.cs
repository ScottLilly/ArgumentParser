using System;
using System.Collections.Generic;
using System.Linq;

namespace ArgumentParser
{
    /// <summary>
    /// Builds an ArgumentSchema. A declaration that cannot work is rejected as it is made,
    /// rather than at Build or at parse time, so the exception points at the offending call.
    /// </summary>
    internal class ArgumentSchemaBuilder : IArgumentSchemaBuilder
    {
        private static readonly string[] s_defaultArgSeparators = { " " };
        private static readonly string[] s_defaultKeyValueSeparators = { ":", "=" };
        private static readonly string[] s_defaultOptionPrefixes = { "--", "-" };

        private readonly List<OptionDefinition> _options = new List<OptionDefinition>();

        private string[] _argSeparators = s_defaultArgSeparators;
        private string[] _keyValueSeparators = s_defaultKeyValueSeparators;
        private string[] _optionPrefixes = s_defaultOptionPrefixes;
        private IEqualityComparer<string> _comparer = StringComparer.OrdinalIgnoreCase;

        public IArgumentSchemaBuilder Option<T>(string name, string alias = null,
            bool required = false, bool repeatable = false, T defaultValue = default,
            string description = null) =>
            Option(name, alias == null ? new string[0] : new[] { alias },
                required, repeatable, defaultValue, description);

        public IArgumentSchemaBuilder Option<T>(string name, string[] aliases,
            bool required = false, bool repeatable = false, T defaultValue = default,
            string description = null)
        {
            if (!OptionValueConverter.IsSupported(typeof(T)))
            {
                throw new ArgumentException(
                    $"{typeof(T).Name} is not a supported option type. Use string, bool, int, long, decimal, double, or an enum.",
                    nameof(T));
            }

            return Add(new OptionDefinition(name, aliases, typeof(T), false, required,
                repeatable, defaultValue, description));
        }

        public IArgumentSchemaBuilder Flag(string name, string alias = null,
            string description = null) =>
            Flag(name, alias == null ? new string[0] : new[] { alias }, description);

        public IArgumentSchemaBuilder Flag(string name, string[] aliases,
            string description = null) =>
            Add(new OptionDefinition(name, aliases, typeof(bool), true, false, false, false,
                description));

        public IArgumentSchemaBuilder WithArgumentSeparators(params string[] argumentSeparators)
        {
            _argSeparators = UseOrKeepDefault(argumentSeparators, s_defaultArgSeparators);

            return this;
        }

        public IArgumentSchemaBuilder WithKeyValueSeparators(params char[] keyValueSeparators)
        {
            _keyValueSeparators = UseOrKeepDefault(
                keyValueSeparators?.Select(c => c.ToString()).ToArray(),
                s_defaultKeyValueSeparators);

            return this;
        }

        public IArgumentSchemaBuilder WithOptionPrefixes(params string[] optionPrefixes)
        {
            // Longest first, so "--verbose" is recognized by "--" rather than by "-".
            _optionPrefixes = UseOrKeepDefault(optionPrefixes, s_defaultOptionPrefixes)
                .OrderByDescending(p => p.Length)
                .ToArray();

            return this;
        }

        public IArgumentSchemaBuilder WithComparer(IEqualityComparer<string> comparer)
        {
            _comparer = comparer ?? StringComparer.OrdinalIgnoreCase;

            return this;
        }

        public ArgumentSchema Build() =>
            new ArgumentSchema(_options, _argSeparators, _keyValueSeparators, _optionPrefixes,
                _comparer);

        private IArgumentSchemaBuilder Add(OptionDefinition option)
        {
            if (string.IsNullOrWhiteSpace(option.Name))
            {
                throw new ArgumentException("An option needs a name.", nameof(option));
            }

            foreach (string name in option.AllNames())
            {
                OptionDefinition clash = _options.FirstOrDefault(
                    o => o.AllNames().Contains(name, _comparer));

                if (clash != null)
                {
                    throw new ArgumentException(
                        $"'{name}' is already declared, on option '{clash.Name}'.",
                        nameof(option));
                }
            }

            _options.Add(option);

            return this;
        }

        // An empty array is treated as "not configured", so a caller who passes nothing keeps
        // the defaults instead of silently matching no separators at all.
        private static string[] UseOrKeepDefault(string[] configured, string[] fallback) =>
            configured == null || configured.Length == 0 ? fallback : configured;
    }
}
