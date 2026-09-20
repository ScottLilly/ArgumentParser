# ScottLilly.ArgumentParser (NuGet package)
<img align="left" width="75" height="75" style="color:white" src="https://raw.githubusercontent.com/ScottLilly/ArgumentParser/master/ArgumentParser/logo_128.png">
A lightweight C# NuGet package for parsing a string, or an array of strings such as the one handed to <code>Main</code>, into something your program can use.
<br/><br/>
It works two ways: hand it a free-form string and it sorts the arguments into integers, decimals, strings, named key/value pairs and enum values, or declare the options your application accepts and it converts them, applies defaults, reports what does not match, and writes your <code>--help</code> text.

## Project Overview
![Build Status](https://github.com/ScottLilly/ArgumentParser/actions/workflows/build-and-test.yml/badge.svg)
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

`string`, `bool`, `int`, `long`, `decimal`, `double` and any enum can be declared. Enums are matched by name, with the same comparer that matches option names, so by default `--type=sales` finds `Sales`; the numeric form is rejected, since it would let any number match any enum.

A declaration that could never work is rejected as you make it. An option cannot be both required and given a default, since the default could only apply when the option is left out and that is already an error. Every name and alias has to start with one of the option prefixes, which is checked at `Build()` because `WithOptionPrefixes` may come after the declarations.

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

`OptionNotRepeatable` applies to an option that takes a value. A flag is never a repeat error, since `-v -v` is what someone types when they want more of whatever the flag asks for. Every occurrence is recorded, so you can count them:

```csharp
Assert.AreEqual(3, schema.Parse("--output=a.json -v -v -v").AllValuesOf<bool>("--verbose").Count);
```

### Arguments that are not options
Anything that is not option-shaped is handed back untouched, in order, so positional arguments still work:

```csharp
SchemaParseResult result = schema.Parse("input.txt --output=a.json other.txt");

CollectionAssert.AreEqual(new[] { "input.txt", "other.txt" }, result.PositionalArguments.ToArray());
```

An argument counts as option-shaped if it starts with `--` or `-` (change that with `WithOptionPrefixes`) and is not a negative number, so `-5` is a positional argument rather than an unknown option.

A bare `--` ends the options. Everything after it is positional whatever it looks like, which is the only way to pass a positional argument that does start with a prefix:

```csharp
SchemaParseResult result = schema.Parse("--output=a.json -- --verbose --notanoption");

Assert.IsFalse(result.IsSet("--verbose"));
CollectionAssert.AreEqual(
    new[] { "--verbose", "--notanoption" }, result.PositionalArguments.ToArray());
```

The marker itself is consumed, and a second `--` after it is an ordinary positional argument. It is tied to the configured prefixes, so a schema built with `WithOptionPrefixes("/")` leaves `--` alone. The untyped `Parser` does not have the concept: `--` there is a string argument like any other.

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

Asking for help suppresses everything else, so `myapp --help` does not complain that `--output` is missing. Descriptions wrap at a fixed width (80 by default, `HelpText(100)` to change it) rather than at the console's, so redirected output is stable. Whitespace in a description is collapsed, so a newline or a tab in one is a word break rather than something that breaks the column layout. If you want the names for yourself, use `WithoutHelpOption()`. When the usage line is only the application name, `WithApplicationName("myapp")` builds `Usage: myapp [options]` for you instead of `WithUsage`.

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

### A named argument's key has to carry a prefix
`Parser` has nothing to check a key against, so the prefix is what tells a named argument from a value that happens to contain a separator. A key that does not start with `--` or `-` is not a named argument, which is what keeps a Windows path out of them:

```csharp
var parsedArguments = new Parser().Parse(@"C:\Test\file.txt");

Assert.AreEqual(0, parsedArguments.NamedArguments.Count);
Assert.AreEqual(@"C:\Test\file.txt", parsedArguments.StringArguments[0]);
```

An unprefixed key is a string argument too:

```csharp
var parsedArguments = new Parser().Parse("db=MyDb");

Assert.AreEqual(0, parsedArguments.NamedArguments.Count);
Assert.AreEqual("db=MyDb", parsedArguments.StringArguments[0]);
```

Pass an empty array of prefixes to accept any key, which is what 1.x did:

```csharp
var parser = new Parser(namedArgumentPrefixes: new string[0]);

Assert.AreEqual("MyDb", parser.Parse("db=MyDb").NamedArguments["db"]);
```

Or name the prefixes your application actually uses:

```csharp
var parser = new Parser(namedArgumentPrefixes: new[] { "/" });

Assert.AreEqual("a.json", parser.Parse("/out:a.json").NamedArguments["/out"]);
```

The fluent builder takes the same thing through `WithNamedArgumentPrefixes`. Stripping the prefix by making it an argument separator still works, described above: the prefix is gone from the key by the time the parser sees it, but the argument still followed one, and that is what counts.

This changed in 2.0.0. Before then, anything containing a key/value separator was a named argument.

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

The comparer governs enum values too, so a schema that is strict about the case of its option names is strict about the case of its values:

```csharp
ArgumentSchema schema =
    ArgumentSchema.Create()
        .WithComparer(StringComparer.Ordinal)
        .Option<EmployeeType>("--type")
        .Build();

Assert.IsTrue(schema.Parse("--type=Sales").Success);
Assert.AreEqual(ParseErrorKind.UnconvertibleValue, schema.Parse("--type=sales").Errors[0].Kind);
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

### Leading and trailing whitespace is trimmed
Every argument, and every named argument's value, is trimmed at both ends, quoted or not. Whitespace inside a value is left exactly as it was typed, including a run of several spaces:

```csharp
var parsedArguments = new Parser().Parse("--name=\"  a  b  \"");

Assert.AreEqual("a  b", parsedArguments.NamedArguments["--name"]);
```

`ArgumentSchema` does the same. Quoting is for the whitespace in the middle of a value; there is no way to keep whitespace at either end of one.

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
