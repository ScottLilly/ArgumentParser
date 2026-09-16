namespace ArgumentParser;

/// <summary>
/// Builds an ArgumentSchema. A declaration that cannot work is rejected as it is made,
/// rather than at parse time, so the exception points at the offending call. The one
/// exception is the check that every name carries an option prefix, which has to wait for
/// Build because WithOptionPrefixes may be called after the options are declared.
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
    private string? _applicationName;
    private string? _description;
    private string? _usage;
    private bool _includeHelpOption = true;

    public IArgumentSchemaBuilder Option<T>(string name, string? alias = null,
        bool required = false, bool repeatable = false,
        OptionDefault<T> defaultValue = default, string? description = null,
        string? valueName = null) =>
        Option(name, alias == null ? new string[0] : new[] { alias },
            required, repeatable, defaultValue, description, valueName);

    public IArgumentSchemaBuilder Option<T>(string name, string[] aliases,
        bool required = false, bool repeatable = false,
        OptionDefault<T> defaultValue = default, string? description = null,
        string? valueName = null)
    {
        // NotSupportedException rather than ArgumentException: the problem is the type
        // argument, and there is no parameter to name.
        if (!OptionValueConverter.IsSupported(typeof(T)))
        {
            throw new NotSupportedException(
                $"{typeof(T).Name} is not a supported option type. Use string, bool, int, long, decimal, double, or an enum.");
        }

        return Add(new OptionDefinition(name, aliases, typeof(T), false, required,
            repeatable, defaultValue.HasValue ? (object?)defaultValue.Value : null,
            description, valueName));
    }

    public IArgumentSchemaBuilder Flag(string name, string? alias = null,
        string? description = null) =>
        Flag(name, alias == null ? new string[0] : new[] { alias }, description);

    public IArgumentSchemaBuilder Flag(string name, string[] aliases,
        string? description = null) =>
        Add(new OptionDefinition(name, aliases, typeof(bool), true, false, false, false,
            description, null));

    public IArgumentSchemaBuilder WithArgumentSeparators(params string[]? argumentSeparators)
    {
        _argSeparators = UseOrKeepDefault(argumentSeparators, s_defaultArgSeparators);

        return this;
    }

    public IArgumentSchemaBuilder WithKeyValueSeparators(params char[]? keyValueSeparators)
    {
        _keyValueSeparators = UseOrKeepDefault(
            keyValueSeparators?.Select(c => c.ToString()).ToArray(),
            s_defaultKeyValueSeparators);

        return this;
    }

    public IArgumentSchemaBuilder WithOptionPrefixes(params string[]? optionPrefixes)
    {
        // Longest first, so "--verbose" is recognized by "--" rather than by "-".
        _optionPrefixes = UseOrKeepDefault(optionPrefixes, s_defaultOptionPrefixes)
            .OrderByDescending(p => p.Length)
            .ToArray();

        return this;
    }

    public IArgumentSchemaBuilder WithComparer(IEqualityComparer<string>? comparer)
    {
        _comparer = comparer ?? StringComparer.OrdinalIgnoreCase;

        return this;
    }

    public IArgumentSchemaBuilder WithApplicationName(string? applicationName)
    {
        _applicationName = applicationName;

        return this;
    }

    public IArgumentSchemaBuilder WithDescription(string? description)
    {
        _description = description;

        return this;
    }

    public IArgumentSchemaBuilder WithUsage(string? usage)
    {
        _usage = usage;

        return this;
    }

    public IArgumentSchemaBuilder WithoutHelpOption()
    {
        _includeHelpOption = false;

        return this;
    }

    // Build leaves the builder as it found it, so calling it twice gives two schemas that
    // behave the same. The help option in particular is not added to _options: it once
    // was, and the second Build then found "--help" already taken and left it out.
    public ArgumentSchema Build()
    {
        CheckNamesArePrefixed();

        OptionDefinition? helpOption = _includeHelpOption ? MakeHelpOption() : null;

        IEnumerable<OptionDefinition> options = helpOption == null
            ? _options
            : _options.Concat(new[] { helpOption });

        return new ArgumentSchema(options, _argSeparators, _keyValueSeparators,
            _optionPrefixes, _comparer, _applicationName, _description, _usage, helpOption);
    }

    // A name with no prefix is never option-shaped, so "x" on its own would be read as a
    // positional argument while "x=1" matched the option. That is not a useful way to
    // declare anything, and there is no configuration that makes it one.
    //
    // The automatic help option is left out deliberately. It is the library's declaration
    // rather than the caller's, and it is matched by name before anything asks what shape
    // it is, so "--help" still works on a schema built with WithOptionPrefixes("/").
    private void CheckNamesArePrefixed()
    {
        foreach (OptionDefinition option in _options)
        {
            foreach (string name in option.AllNames())
            {
                bool hasPrefix = _optionPrefixes.Any(prefix =>
                    name.Length > prefix.Length
                    && name.StartsWith(prefix, StringComparison.Ordinal));

                if (hasPrefix)
                {
                    continue;
                }

                throw new ArgumentException(
                    $"'{name}' does not start with an option prefix ({string.Join(", ", _optionPrefixes)}), so it could never be matched as an option.");
            }
        }
    }

    // Goes last, so it reads as the final line of the help text. Skipped entirely if the
    // caller has already used either name for something of their own, since taking it from
    // them silently would be worse than having no automatic help.
    private OptionDefinition? MakeHelpOption()
    {
        string[] names = { "--help", "-h" };

        if (names.Any(name => _options.Any(o => o.AllNames().Contains(name, _comparer))))
        {
            return null;
        }

        return new OptionDefinition("--help", new[] { "-h" },
            typeof(bool), true, false, false, false, "Show this help", null);
    }

    private IArgumentSchemaBuilder Add(OptionDefinition option)
    {
        if (string.IsNullOrWhiteSpace(option.Name))
        {
            throw new ArgumentException("An option needs a name.", nameof(option));
        }

        // A required option is an error when it is left out, which is the only time a
        // default could apply, so the two together describe something that cannot happen.
        // Ignoring the default silently would leave the declaration reading as though it
        // meant something.
        if (option.IsRequired && option.DefaultValue != null)
        {
            throw new ArgumentException(
                $"Option '{option.Name}' is required, so its default value of '{option.DefaultValue}' could never be used. Declare it as required, or give it a default, but not both.",
                nameof(option));
        }

        foreach (string name in option.AllNames())
        {
            OptionDefinition? clash = _options.FirstOrDefault(
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
    private static string[] UseOrKeepDefault(string[]? configured, string[] fallback) =>
        configured == null || configured.Length == 0 ? fallback : configured;
}
