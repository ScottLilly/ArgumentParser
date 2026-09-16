# ScottLilly.ArgumentParser (NuGet package)
<img align="left" width="75" height="75" style="color:white" src="https://raw.githubusercontent.com/ScottLilly/ArgumentParser/master/ArgumentParser/logo_128.png">
A lightweight C# NuGet package for parsing a string, or an array of strings such as the one handed to <code>Main</code>, into something your program can use.
<br/><br/>
It works two ways: hand it a free-form string and it sorts the arguments into integers, decimals, strings, named key/value pairs and enum values, or declare the options your application accepts and it converts them, applies defaults, reports what does not match, and writes your <code>--help</code> text.

## Project Overview
![Build Status](https://github.com/ScottLilly/ArgumentParser/actions/workflows/ci.yml/badge.svg)
[![NuGet](https://img.shields.io/nuget/v/ScottLilly.ArgumentParser)](https://www.nuget.org/packages/ScottLilly.ArgumentParser/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ScottLilly.ArgumentParser)](https://www.nuget.org/packages/ScottLilly.ArgumentParser/)
[![License](https://img.shields.io/github/license/ScottLilly/ArgumentParser)](https://github.com/ScottLilly/ArgumentParser/blob/master/LICENSE.txt)

## Installation
Install the package via NuGet Package Manager or use the following command in the Package Manager Console:

```powershell
Install-Package ScottLilly.ArgumentParser
```
Or via the .NET CLI:
```bash
dotnet add package ScottLilly.ArgumentParser
```

## Which one you want
There are two entry points, and they suit different jobs.

| | Use it when |
|---|---|
| `Parser` | You are parsing a free-form string and want whatever is in it, sorted by type. Nothing is declared ahead of time, so nothing can be reported as wrong. |
| `ArgumentSchema` | Your application has a known set of options. Declare them and you get type conversion, defaults, required checks, aliases, unknown-option detection and generated `--help`. |

`Parser` came first and is documented next. [Declaring your options](#declaring-your-options) covers the schema.

## How to use `Parser`
Instantiate a `Parser` to turn a string, or an array of strings, into a `ParsedArguments` object. The constructor optionally takes the characters or strings that separate one argument from the next, the characters that separate a name from its value, and a comparer for matching names.

### Code samples:

### Parse a string with various argument types
```csharp
var parser = new Parser();

var parsedArguments = parser.Parse("123 45.67 hello world --key=value");

Assert.AreEqual(5, parsedArguments.Arguments.Count);
Assert.AreEqual(1, parsedArguments.IntegerArguments.Count);
Assert.AreEqual(1, parsedArguments.DecimalArguments.Count);
Assert.AreEqual(2, parsedArguments.StringArguments.Count);
Assert.AreEqual(1, parsedArguments.NamedArguments.Count);
Assert.AreEqual("value", parsedArguments.NamedArguments["--key"]);
```
Note that the key is `"--key"` rather than `"key"`. See [Named argument keys keep their prefix](#named-argument-keys-keep-their-prefix) below.

### Parse values that should match an enum type
```csharp
public enum EmployeeType
{
    Production,
    Sales,
    Marketing,
}

var parser = new Parser();

var parsedArguments =
    parser.Parse("production sales marketing");

Assert.AreEqual(3, parsedArguments.Arguments.Count);
Assert.AreEqual(0, parsedArguments.IntegerArguments.Count);
Assert.AreEqual(0, parsedArguments.DecimalArguments.Count);
Assert.AreEqual(3, parsedArguments.StringArguments.Count);
Assert.AreEqual(3, parsedArguments.EnumArgumentsOfType<EmployeeType>().Count());
```

### Use fluent interface to parse arguments
```csharp
ParsedArguments parsedArguments =
    FluentArgumentParser
    .Create()
    .AddArgumentSeparators(new string[] { "--", "-" })
    .AddKeyValueSeparators(new char[] { ':', '|' })
    .Parse(@"--solution:value1 -s|value2");

Assert.AreEqual(2, parsedArguments.Arguments.Count);
Assert.AreEqual("value1", parsedArguments.NamedArguments["solution"]);
Assert.AreEqual("value2", parsedArguments.NamedArguments["s"]);
```

### Use fluent interface to initialize parser, then parse arguments
```csharp
var initializedParser = 
    FluentArgumentParser
    .Create()
    .AddArgumentSeparators(new string[] { "--", "-" })
    .AddKeyValueSeparators(new char[] { ':', '|' });

ParsedArguments parsedArguments =
    initializedParser.Parse(@"--solution:value1 -s|value2");

Assert.AreEqual(2, parsedArguments.Arguments.Count);
Assert.AreEqual("value1", parsedArguments.NamedArguments["solution"]);
Assert.AreEqual("value2", parsedArguments.NamedArguments["s"]);
```

## Declaring your options

Everything above parses whatever it finds and hands you buckets of values. If your application knows what its options are, declare them instead and the parser can do considerably more: catch a misspelled option, convert values to the type you asked for, apply defaults, enforce required options, and accept a value written either way round.

```csharp
ArgumentSchema schema =
    ArgumentSchema.Create()
        .Option<string>("--output", alias: "-o", required: true, repeatable: true,
            description: "Where to write the report")
        .Option<int>("--timeout", defaultValue: 60,
            description: "Seconds before the run is abandoned")
        .Flag("--verbose", alias: "-v")
        .Build();

SchemaParseResult result = schema.Parse(args);

if (!result.Success)
{
    Console.Error.WriteLine(result.ErrorText());
    return 1;
}

int timeout = result.ValueOf<int>("--timeout");
bool verbose = result.ValueOf<bool>("--verbose");
IReadOnlyList<string> outputs = result.AllValuesOf<string>("--output");
```

`string`, `bool`, `int`, `long`, `decimal`, `double` and any enum can be declared. Enums are matched by name, ignoring case; the numeric form is rejected, since it would let any number match any enum.

`ValueOf` falls back to the declared default when the option was not given. `IsSet` tells you whether it was given at all, which is how to tell "not given" from "given the same value as the default":

```csharp
Assert.IsFalse(schema.Parse("--output=a.json").IsSet("--timeout"));
Assert.IsTrue(schema.Parse("--output=a.json --timeout=60").IsSet("--timeout"));
```

### A declaration changes how arguments are read
Because the schema knows `--timeout` takes a value, you no longer have to configure a separator for it. These are all the same:

```csharp
schema.Parse("--timeout 30");
schema.Parse("--timeout=30");
schema.Parse("--timeout:30");
```

A flag needs no value at all, and an option whose value was left out is reported rather than swallowing the next option:

```csharp
// "--output --verbose" is a mistake, not a request to write to a file called "--verbose".
SchemaParseResult result = schema.Parse("--output --verbose");

Assert.IsFalse(result.Success);
Assert.AreEqual(ParseErrorKind.MissingValue, result.Errors[0].Kind);
```

### Everything wrong is reported at once
A command line application usually wants to print every problem rather than making the user fix them one run at a time, so `Parse` collects them all:

```csharp
SchemaParseResult result = schema.Parse("--timeout=abc --verbse");

// Unknown option '--verbse'.
// Option '--output' is required.
// Option '--timeout' needs a whole number, but was given 'abc'.
Assert.AreEqual(3, result.Errors.Count);
```

The kinds are `UnknownOption`, `MissingValue`, `UnconvertibleValue`, `MissingRequiredOption` and `OptionNotRepeatable`. If you would rather handle one exception than check `Success`, use `ParseOrThrow`, which throws an `ArgumentParseException` carrying the same list.

### Arguments that are not options
Anything that is not option-shaped is handed back untouched, in order, so positional arguments still work:

```csharp
SchemaParseResult result = schema.Parse("input.txt --output=a.json other.txt");

CollectionAssert.AreEqual(new[] { "input.txt", "other.txt" }, result.PositionalArguments.ToArray());
```

An argument counts as option-shaped if it starts with `--` or `-` (change that with `WithOptionPrefixes`) and is not a negative number, so `-5` is a positional argument rather than an unknown option.

### Help text writes itself
`--help` and `-h` are recognized automatically and reported through the result, so nothing is printed unless you print it. The text is built from the same declarations that parse the arguments, so the two cannot disagree:

```csharp
ArgumentSchema schema =
    ArgumentSchema.Create()
        .WithDescription("Checks a solution and writes a report.")
        .WithUsage("myapp <input> [options]")
        .Option<string>("--output", alias: "-o", required: true, repeatable: true,
            description: "Where to write the report", valueName: "path")
        .Option<int>("--timeout", defaultValue: 60,
            description: "Seconds before the run is abandoned")
        .Flag("--verbose", alias: "-v", description: "Print each step as it runs")
        .Build();

SchemaParseResult result = schema.Parse(args);

if (result.HelpRequested)
{
    Console.WriteLine(schema.HelpText());
    return 0;
}
```

`schema.HelpText()` gives you:

```
Checks a solution and writes a report.

Usage: myapp <input> [options]

  -o, --output <path>  Where to write the report (required, repeatable)
      --timeout <int>  Seconds before the run is abandoned (default: 60)
  -v, --verbose        Print each step as it runs
  -h, --help           Show this help
```

Asking for help suppresses everything else, so `myapp --help` does not complain that `--output` is missing. Descriptions wrap at a fixed width (80 by default, `HelpText(100)` to change it) rather than at the console's, so redirected output is stable. If you want the names for yourself, use `WithoutHelpOption()`. When the usage line is only the application name, `WithApplicationName("myapp")` builds `Usage: myapp [options]` for you instead of `WithUsage`.

The untyped `Parser` is still the right tool when you are parsing a free-form string rather than a known set of options.

## Things worth knowing

### Named argument keys keep their prefix
The parser splits a named argument at the key/value separator and keeps everything to the left of it, so `--key=value` gives you the key `"--key"`. Looking it up as `"key"` throws a `KeyNotFoundException`.

If you want the prefix stripped, make it an argument separator instead:

```csharp
var parser = new Parser(new[] { "--", "-" }, new[] { ':' });

var parsedArguments = parser.Parse("--key:value");

Assert.AreEqual("value", parsedArguments.NamedArguments["key"]);
```

That idiom has a sharp edge: once `"-"` or `"--"` is an argument separator, a hyphen inside a value splits the argument too.

```csharp
var parser = new Parser(new[] { "--", "-" }, new[] { ':' });

var parsedArguments = parser.Parse("--branch:release-1.2");

// Two arguments, not one. The value is cut at the hyphen.
Assert.AreEqual("release", parsedArguments.NamedArguments["branch"]);
Assert.AreEqual(1.2m, parsedArguments.DecimalArguments[0]);
```

Quoting the value avoids it, since a quoted section is never split on:

```csharp
var parsedArguments = parser.Parse(@"--branch:""release-1.2""");

Assert.AreEqual("release-1.2", parsedArguments.NamedArguments["branch"]);
```

If you cannot rely on callers quoting, keep the prefix on the key instead.

### A bare Windows path becomes a named argument
`Parser` has nothing to check a key against, so anything with a key/value separator in it is a named argument. With the default `:` separator that includes a drive letter:

```csharp
var parsedArguments = new Parser().Parse(@"C:\Test\file.txt");

Assert.AreEqual(@"\Test\file.txt", parsedArguments.NamedArguments["C"]);
```

If your arguments include paths, either drop `:` from the key/value separators (`new Parser(keyValueSeparators: new[] { '=' })`) or declare your options with `ArgumentSchema`, which only splits at a separator when the key is a declared option.

### Names are matched without regard to case
`--output` and `--Output` are the same argument. Pass `StringComparer.Ordinal` if you want them treated as two:

```csharp
var parser = new Parser(comparer: StringComparer.Ordinal);
```

The schema takes the same comparer through `WithComparer`, and reports the other casing as an unknown option:

```csharp
ArgumentSchema schema =
    ArgumentSchema.Create()
        .WithComparer(StringComparer.Ordinal)
        .Flag("--verbose")
        .Build();

Assert.IsTrue(schema.Parse("--verbose").Success);
Assert.AreEqual(ParseErrorKind.UnknownOption, schema.Parse("--Verbose").Errors[0].Kind);
```

This changed in 2.0.0. Before then, names were always matched case-sensitively.

### A name given more than once keeps the last value
`NamedArguments` holds the last value, which is what most command line applications do with a repeated option. `AllValuesOf` returns every value, in the order given, so nothing you typed is lost:

```csharp
var parsedArguments = new Parser().Parse("--exclude=bin --exclude=obj");

Assert.AreEqual("obj", parsedArguments.NamedArguments["--exclude"]);
CollectionAssert.AreEqual(new[] { "bin", "obj" }, parsedArguments.AllValuesOf("--exclude").ToArray());
```

### Double quotes group a value
A quoted section is not split on, so a value can contain a separator. The quotes group the value and are removed from it:

```csharp
var parsedArguments = new Parser().Parse(@"--solution=""C:\Test\My Project.sln""");

Assert.AreEqual(@"C:\Test\My Project.sln", parsedArguments.NamedArguments["--solution"]);
```

There is no escape sequence, so a value cannot itself contain a double quote.

## Requirements
- .NET Standard 2.0 or .NET 8.0. The package ships both, so it runs on .NET Framework 4.6.1 and later, .NET Core 2.0 and later, and .NET 5 and later.
- No external dependencies.
- The public API is annotated for nullable reference types, and ships XML doc comments.

## Contributing
Contributions are welcome. Please submit issues or pull requests to the GitHub repository.

## License
This project is licensed under the MIT License. See the [LICENSE file](https://github.com/ScottLilly/ArgumentParser/blob/master/LICENSE.txt) for details.

## Contact
For questions or feedback, please [open an issue here on GitHub](https://github.com/ScottLilly/ArgumentParser/issues).
