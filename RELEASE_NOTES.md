# Release notes

The section for the version being packed is read into the NuGet package's release notes by
`ArgumentParser.csproj`, and into the GitHub Release by `release.yml`. Keep each heading in the
form `## Version x.y.z`, matching `<Version>` in the csproj, newest first.

## Version 2.0.0

Breaking changes:

- Named argument names are matched case-insensitively by default, so "--output" and "--Output" are the same argument. Pass StringComparer.Ordinal to the Parser constructor, or WithComparer on the fluent builder, for the 1.x behavior.
- Numbers are classified with the invariant culture, so the same command line means the same thing on every machine. An argument counts as a number only if it is digits with an optional leading sign and, for decimals, a single period. Group separators, trailing signs and exponents ("1,234", "5-", "1e5") are string arguments.
- A key/value argument splits at the leftmost separator rather than the first separator in the configured order, so "--out=C:\build" splits at the "=" and keeps "C:\build" whole.
- Double quotes group a value and are removed from it, so a value can contain a separator: --solution="C:\My Project.sln". There is no escape sequence, so a value cannot contain a double quote.
- EnumArgumentsOfType requires an enum type. Calling it with a non-enum struct is now a compile error rather than a run time ArgumentException.
- Null input returns an empty result instead of throwing.
- A fluent builder with no separators configured uses the same defaults as the Parser constructor, instead of matching no named arguments.
- FluentArgumentParser's parameterless constructor is private. Create() was always the intended entry point, and it and every chaining method return the interface, so "new FluentArgumentParser()" was a way in that nothing else used.

Added:

- XML doc comments, shipped as an .xml beside each DLL, so the whole public API shows its documentation in IntelliSense instead of bare signatures.
- Nullable reference type annotations on the whole public API, so a consumer building with nullable enabled gets real compiler help instead of seeing everything as oblivious. The optional constructor parameters now read as deliberately optional, and Parse says in its signature that it accepts null.
- A .NET 8 target alongside netstandard2.0. The package now ships lib/netstandard2.0 and lib/net8.0, and netstandard2.0 is still there for reach.
- A declared option schema. ArgumentSchema.Create() declares each option's name, aliases, type, default, and whether it is required or repeatable, and schema.Parse returns a SchemaParseResult with converted values. Because the options are declared, a value can be written either way round ("--timeout 30" as well as "--timeout=30"), a flag needs no value, and enums are converted by name. The untyped Parser still suits a free-form string.
- Help and usage text generated from the declared options, so it cannot drift from what the application actually accepts. "--help" and "-h" are recognized automatically and reported through the result rather than written to the console, so the caller decides where the text goes. Descriptions wrap at a fixed width rather than the console's, so redirected output is stable.
- Error reporting for a schema parse. Unknown options, missing values, unconvertible values, missing required options and unexpected repeats are all reported, and every problem is collected rather than only the first. Read them from SchemaParseResult.Errors, or use ParseOrThrow for an ArgumentParseException carrying the same list.
- ParsedArguments.AllValuesOf(name) returns every value given for a repeated name, in the order given. NamedArguments still holds the last one.
- An optional comparer for named argument names, on both Parser constructors and as WithComparer on the fluent builder.
