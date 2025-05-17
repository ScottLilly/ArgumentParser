# ScottLilly.ArgumentParser
A lightweight C# NuGet package for parsing a string, or array of strings (such as command-line arguments) into a ParsedArguments object. It categorizes arguments into their types, including all arguments, integers, decimals, strings, named key/value pairs, and enum-based arguments.

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

## Features
Instantiate a `Parser` object to parse strings or arrays of strings into a `ParsedArguments` object. 
Pass the optional array of characters or strings to use to separate arguments in the string and/or to separate key/value pair arguments.

## How to use
Code samples:

### Parse a string with various argument types
```
var parser = new Parser();

var parsedArguments = parser.Parse("123 45.67 hello world --key=value");

Assert.Equal(5, parsedArguments.Arguments.Count);
Assert.Equal(1, parsedArguments.IntegerArguments.Count);
Assert.Equal(1, parsedArguments.DecimalArguments.Count);
Assert.Equal(3, parsedArguments.StringArguments.Count);
Assert.Equal(1, parsedArguments.NamedArguments.Count);
Assert.Equal("value", parsedArguments.NamedArguments["key"]);
```

### Parse values that should match an enum type
```
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
```
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
```
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

## Requirements
- .NET Standard 2.0 or higher
- No external dependencies.

## Contributing
Contributions are welcome. Please submit issues or pull requests to the GitHub repository.

## License
This project is licensed under the MIT License. See the [LICENSE file](https://github.com/ScottLilly/ArgumentParser/blob/master/LICENSE.txt) for details.

## Contact
For questions or feedback, please [open an issue here on GitHub](https://github.com/ScottLilly/ArgumentParser/issues).
