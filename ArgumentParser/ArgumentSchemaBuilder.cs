namespace ArgumentParser;

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
        if (!OptionValueConverter.IsSupported(typeof(T)))
        {
            throw new ArgumentException(
                $"{typeof(T).Name} is not a supported option type. Use string, bool, int, long, decimal, double, or an enum.",
                nameof(T));
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
        OptionDefinition? helpOption = _includeHelpOption ? MakeHelpOption() : null;

        IEnumerable<OptionDefinition> options = helpOption == null
            ? _options
            : _options.Concat(new[] { helpOption });

        return new ArgumentSchema(options, _argSeparators, _keyValueSeparators,
            _optionPrefixes, _comparer, _applicationName, _description, _usage, helpOption);
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
