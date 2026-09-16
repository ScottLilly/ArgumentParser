# ScottLilly.ArgumentParser (NuGet package)
<img align="left" width="75" height="75" style="color:white" src="https://github.com/ScottLilly/ArgumentParser/blob/master/ArgumentParser/logo_128.png">
A lightweight C# NuGet package for parsing a string, or array of strings (such as command-line arguments) into a ParsedArguments object.
<br/><br/>
It categorizes arguments into their types, including all arguments, integers, decimals, strings, named key/value pairs, and enum-based arguments.

## Project Overview
![Build Status](https://github.com/ScottLilly/ArgumentParser/actions/workflows/ci.yml/badge.svg)
[![NuGet](https://img.shields.io/nuget/v/ScottLilly.ArgumentParser)](https://www.nuget.org/packages/ScottLilly.ArgumentParser/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ScottLilly.ArgumentParser)](https://www.nuget.org/packages/ScottLilly.ArgumentParser/)
[![License](https://img.shields.io/github/license/ScottLilly/ArgumentParser)](https://github.com/ScottLilly/ArgumentParser/LICENSE)

## Installation
Install the package via NuGet Package Manager or use the following command in the Package Manager Console:

```
Install-Package ScottLilly.ArgumentParser
```
Or via the .NET CLI:
```
dotnet add package ScottLilly.ArgumentParser
```

## How to use
Instantiate a `Parser` object to parse strings or arrays of strings into a `ParsedArguments` object, 
passing in an optional array of characters or strings to use to separate arguments in the string and/or to separate key/value pair arguments.

### Code samples:

### Parse a string with various argument types
```csharp
var parser = new Parser();

var parsedArguments = parser.Parse("123 45.67 hello world --key=value");

Assert.Equal(5, parsedArguments.Arguments.Count);
Assert.Equal(1, parsedArguments.IntegerArguments.Count);
Assert.Equal(1, parsedArguments.DecimalArguments.Count);
Assert.Equal(2, parsedArguments.StringArguments.Count);
Assert.Equal(1, parsedArguments.NamedArguments.Count);
Assert.Equal("value", parsedArguments.NamedArguments["--key"]);
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

Assert.Equal(3, parsedArguments.Arguments.Count);
Assert.Empty(parsedArguments.IntegerArguments);
Assert.Empty(parsedArguments.DecimalArguments);
Assert.Equal(3, parsedArguments.StringArguments.Count);
Assert.Equal(3, parsedArguments.EnumArgumentsOfType<EmployeeType>().Count());
```

### Use fluent interface to parse arguments
```csharp
ParsedArguments parsedArguments =
    FluentArgumentParser
    .Create()
    .AddArgumentSeparators(new string[] { "--", "-" })
    .AddKeyValueSeparators(new char[] { ':', '|' })
    .Parse(@"--solution:value1 -s|value2");

Assert.Equal(2, parsedArguments.Arguments.Count);
Assert.Equal("value1", parsedArguments.NamedArguments["solution"]);
Assert.Equal("value2", parsedArguments.NamedArguments["s"]);
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

Assert.Equal(2, parsedArguments.Arguments.Count);
Assert.Equal("value1", parsedArguments.NamedArguments["solution"]);
Assert.Equal("value2", parsedArguments.NamedArguments["s"]);
```

## Things worth knowing

### Named argument keys keep their prefix
The parser splits a named argument at the key/value separator and keeps everything to the left of it, so `--key=value` gives you the key `"--key"`. Looking it up as `"key"` throws a `KeyNotFoundException`.

If you want the prefix stripped, make it an argument separator instead:

```csharp
var parser = new Parser(new[] { "--", "-" }, new[] { ':' });

var parsedArguments = parser.Parse("--key:value");

Assert.Equal("value", parsedArguments.NamedArguments["key"]);
```

That idiom has a sharp edge: once `"-"` or `"--"` is an argument separator, a hyphen inside a value splits the argument too.

```csharp
var parser = new Parser(new[] { "--", "-" }, new[] { ':' });

var parsedArguments = parser.Parse("--branch:release-1.2");

// Two arguments, not one. The value is cut at the hyphen.
Assert.Equal("release", parsedArguments.NamedArguments["branch"]);
Assert.Equal(1.2m, parsedArguments.DecimalArguments[0]);
```

Quoting the value avoids it, since a quoted section is never split on:

```csharp
var parsedArguments = parser.Parse(@"--branch:""release-1.2""");

Assert.Equal("release-1.2", parsedArguments.NamedArguments["branch"]);
```

If you cannot rely on callers quoting, keep the prefix on the key instead.

### Names are matched without regard to case
`--output` and `--Output` are the same argument. Pass `StringComparer.Ordinal` if you want them treated as two:

```csharp
var parser = new Parser(comparer: StringComparer.Ordinal);
```

This changed in 2.0.0. Before then, names were always matched case-sensitively.

### A name given more than once keeps the last value
`NamedArguments` holds the last value, which is what most command line applications do with a repeated option. `AllValuesOf` returns every value, in the order given, so nothing you typed is lost:

```csharp
var parsedArguments = new Parser().Parse("--exclude=bin --exclude=obj");

Assert.Equal("obj", parsedArguments.NamedArguments["--exclude"]);
Assert.Equal(new[] { "bin", "obj" }, parsedArguments.AllValuesOf("--exclude"));
```

### Double quotes group a value
A quoted section is not split on, so a value can contain a separator. The quotes group the value and are removed from it:

```csharp
var parsedArguments = new Parser().Parse(@"--solution=""C:\Test\My Project.sln""");

Assert.Equal(@"C:\Test\My Project.sln", parsedArguments.NamedArguments["--solution"]);
```

There is no escape sequence, so a value cannot itself contain a double quote.

## Requirements
- .NET Standard 2.0 or higher
- No external dependencies.

## Contributing
Contributions are welcome. Please submit issues or pull requests to the GitHub repository.

## License
This project is licensed under the MIT License. See the [LICENSE file](https://github.com/ScottLilly/ArgumentParser/blob/master/LICENSE.txt) for details.

## Contact
For questions or feedback, please [open an issue here on GitHub](https://github.com/ScottLilly/ArgumentParser/issues).
