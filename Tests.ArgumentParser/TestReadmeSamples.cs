using ArgumentParser;

namespace Tests.ArgumentParser;

/// <summary>
/// Every code sample in README.md, so the documentation cannot drift away from the code
/// again. Issue #45 was the first sample throwing a KeyNotFoundException at anyone who
/// copied it, which nothing caught because nothing ran it.
///
/// Each test is named after the README heading it comes from. A failure here means the
/// README and the code disagree, so fix whichever of the two is wrong rather than only
/// the test. The samples use MSTest assertions, the same as this project, so they can be
/// pasted here as they are.
/// </summary>
[TestClass]
public class TestReadmeSamples
{
    #region Code samples

    // "Parse a string with various argument types"
    [TestMethod]
    public void ParseAStringWithVariousArgumentTypes()
    {
        var parser = new Parser();

        var parsedArguments = parser.Parse("123 45.67 hello world --key=value");

        Assert.AreEqual(5, parsedArguments.Arguments.Count);
        Assert.AreEqual(1, parsedArguments.IntegerArguments.Count);
        Assert.AreEqual(1, parsedArguments.DecimalArguments.Count);
        Assert.AreEqual(2, parsedArguments.StringArguments.Count);
        Assert.AreEqual(1, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("value", parsedArguments.NamedArguments["--key"]);
    }

    // "Parse values that should match an enum type". The README declares EmployeeType inline
    // with the same three values as the shared test enum, which is what this uses.
    [TestMethod]
    public void ParseValuesThatShouldMatchAnEnumType()
    {
        var parser = new Parser();

        var parsedArguments =
            parser.Parse("production sales marketing");

        Assert.AreEqual(3, parsedArguments.Arguments.Count);
        Assert.AreEqual(0, parsedArguments.IntegerArguments.Count);
        Assert.AreEqual(0, parsedArguments.DecimalArguments.Count);
        Assert.AreEqual(3, parsedArguments.StringArguments.Count);
        Assert.AreEqual(3, parsedArguments.EnumArgumentsOfType<EmployeeType>().Count());
    }

    // "Use fluent interface to parse arguments"
    [TestMethod]
    public void UseFluentInterfaceToParseArguments()
    {
        ParsedArguments parsedArguments =
            FluentArgumentParser
            .Create()
            .AddArgumentSeparators(new string[] { "--", "-" })
            .AddKeyValueSeparators(new char[] { ':', '|' })
            .Parse(@"--solution:value1 -s|value2");

        Assert.AreEqual(2, parsedArguments.Arguments.Count);
        Assert.AreEqual("value1", parsedArguments.NamedArguments["solution"]);
        Assert.AreEqual("value2", parsedArguments.NamedArguments["s"]);
    }

    // "Use fluent interface to initialize parser, then parse arguments"
    [TestMethod]
    public void UseFluentInterfaceToInitializeParserThenParseArguments()
    {
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
    }

    #endregion

    #region Declaring your options

    private static ArgumentSchema ReadmeSchema() =>
        ArgumentSchema.Create()
            .Option<string>("--output", alias: "-o", required: true, repeatable: true,
                description: "Where to write the report")
            .Option<int>("--timeout", defaultValue: 60,
                description: "Seconds before the run is abandoned")
            .Flag("--verbose", alias: "-v")
            .Build();

    // The opening sample of the section, minus the Console.Error call and the return.
    [TestMethod]
    public void DeclaringYourOptions()
    {
        ArgumentSchema schema = ReadmeSchema();

        SchemaParseResult result = schema.Parse(new[] { "--output", "a.json", "--verbose" });

        Assert.IsTrue(result.Success, result.ErrorText());

        int timeout = result.ValueOf<int>("--timeout");
        bool verbose = result.ValueOf<bool>("--verbose");
        IReadOnlyList<string> outputs = result.AllValuesOf<string>("--output");

        Assert.AreEqual(60, timeout);
        Assert.IsTrue(verbose);
        CollectionAssert.AreEqual(new[] { "a.json" }, outputs.ToArray());
    }

    // "A declaration changes how arguments are read"
    [TestMethod]
    public void ADeclarationChangesHowArgumentsAreRead()
    {
        ArgumentSchema schema = ReadmeSchema();

        foreach (string arguments in
                 new[] { "--timeout 30", "--timeout=30", "--timeout:30" })
        {
            Assert.AreEqual(30, schema.Parse(arguments).ValueOf<int>("--timeout"), arguments);
        }
    }

    [TestMethod]
    public void AnOptionWhoseValueWasLeftOutIsReported()
    {
        ArgumentSchema schema = ReadmeSchema();

        SchemaParseResult result = schema.Parse("--output --verbose");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ParseErrorKind.MissingValue, result.Errors[0].Kind);
    }

    // "Everything wrong is reported at once", including the three messages in the comment.
    [TestMethod]
    public void EverythingWrongIsReportedAtOnce()
    {
        ArgumentSchema schema = ReadmeSchema();

        SchemaParseResult result = schema.Parse("--timeout=abc --verbse");

        Assert.AreEqual(3, result.Errors.Count);
        CollectionAssert.AreEquivalent(
            new[]
            {
                "Unknown option '--verbse'.",
                "Option '--output' is required.",
                "Option '--timeout' needs a whole number, but was given 'abc'."
            },
            result.Errors.Select(e => e.Message).ToArray());
    }

    // "Arguments that are not options"
    [TestMethod]
    public void ArgumentsThatAreNotOptions()
    {
        ArgumentSchema schema = ReadmeSchema();

        SchemaParseResult result = schema.Parse("input.txt --output=a.json other.txt");

        CollectionAssert.AreEqual(
            new[] { "input.txt", "other.txt" }, result.PositionalArguments.ToArray());
    }

    [TestMethod]
    public void ANegativeNumberIsPositionalRatherThanAnUnknownOption()
    {
        ArgumentSchema schema = ReadmeSchema();

        SchemaParseResult result = schema.Parse("--output=a.json -5");

        Assert.IsTrue(result.Success, result.ErrorText());
        CollectionAssert.AreEqual(new[] { "-5" }, result.PositionalArguments.ToArray());
    }

    // "A bare -- ends the options"
    [TestMethod]
    public void ABareDoubleDashEndsTheOptions()
    {
        ArgumentSchema schema = ReadmeSchema();

        SchemaParseResult result = schema.Parse("--output=a.json -- --verbose --notanoption");

        Assert.IsFalse(result.IsSet("--verbose"));
        CollectionAssert.AreEqual(
            new[] { "--verbose", "--notanoption" }, result.PositionalArguments.ToArray());
    }

    // "A flag is never a repeat error ... Every occurrence is recorded"
    [TestMethod]
    public void ARepeatedFlagIsCounted()
    {
        ArgumentSchema schema = ReadmeSchema();

        Assert.AreEqual(3,
            schema.Parse("--output=a.json -v -v -v").AllValuesOf<bool>("--verbose").Count);
    }

    // "Help text writes itself", including the block showing exactly what HelpText() gives.
    [TestMethod]
    public void HelpTextWritesItself()
    {
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

        SchemaParseResult result = schema.Parse(new[] { "--help" });

        Assert.IsTrue(result.HelpRequested);

        string expected = string.Join(Environment.NewLine,
            "Checks a solution and writes a report.",
            "",
            "Usage: myapp <input> [options]",
            "",
            "  -o, --output <path>  Where to write the report (required, repeatable)",
            "      --timeout <int>  Seconds before the run is abandoned (default: 60)",
            "  -v, --verbose        Print each step as it runs",
            "  -h, --help           Show this help");

        Assert.AreEqual(expected, schema.HelpText());
    }

    #endregion

    #region Named argument keys keep their prefix

    // The claim the section opens with, that looking the key up without its prefix throws.
    [TestMethod]
    public void NamedArgumentKeysKeepTheirPrefix()
    {
        var parsedArguments = new Parser().Parse("--key=value");

        Assert.AreEqual("value", parsedArguments.NamedArguments["--key"]);
        Assert.ThrowsExactly<KeyNotFoundException>(
            () => _ = parsedArguments.NamedArguments["key"]);
    }

    // "If you want the prefix stripped, make it an argument separator instead"
    [TestMethod]
    public void MakingThePrefixAnArgumentSeparatorStripsIt()
    {
        var parser = new Parser(new[] { "--", "-" }, new[] { ':' });

        var parsedArguments = parser.Parse("--key:value");

        Assert.AreEqual("value", parsedArguments.NamedArguments["key"]);
    }

    // "That idiom has a sharp edge"
    [TestMethod]
    public void AHyphenInsideAValueSplitsTheArgument()
    {
        var parser = new Parser(new[] { "--", "-" }, new[] { ':' });

        var parsedArguments = parser.Parse("--branch:release-1.2");

        // Two arguments, not one. The value is cut at the hyphen.
        Assert.AreEqual("release", parsedArguments.NamedArguments["branch"]);
        Assert.AreEqual(1.2m, parsedArguments.DecimalArguments[0]);
    }

    // "Quoting the value avoids it, since a quoted section is never split on"
    [TestMethod]
    public void QuotingAHyphenatedValueKeepsItWhole()
    {
        var parser = new Parser(new[] { "--", "-" }, new[] { ':' });

        var parsedArguments = parser.Parse(@"--branch:""release-1.2""");

        Assert.AreEqual("release-1.2", parsedArguments.NamedArguments["branch"]);
    }

    #endregion

    #region Names are matched without regard to case

    [TestMethod]
    public void NamesAreMatchedWithoutRegardToCase()
    {
        var parsedArguments = new Parser().Parse("--output=a.json");

        Assert.AreEqual("a.json", parsedArguments.NamedArguments["--Output"]);
    }

    // "Pass StringComparer.Ordinal if you want them treated as two"
    [TestMethod]
    public void OrdinalComparerTreatsNamesDifferingInCaseAsTwoArguments()
    {
        var parser = new Parser(comparer: StringComparer.Ordinal);

        var parsedArguments = parser.Parse("--output=a --Output=b");

        Assert.AreEqual(2, parsedArguments.NamedArguments.Count);
    }

    #endregion

    #region A name given more than once keeps the last value

    [TestMethod]
    public void ANameGivenMoreThanOnceKeepsTheLastValue()
    {
        var parsedArguments = new Parser().Parse("--exclude=bin --exclude=obj");

        Assert.AreEqual("obj", parsedArguments.NamedArguments["--exclude"]);
        CollectionAssert.AreEqual(
            new[] { "bin", "obj" }, parsedArguments.AllValuesOf("--exclude").ToArray());
    }

    #endregion

    #region Double quotes group a value

    [TestMethod]
    public void DoubleQuotesGroupAValue()
    {
        var parsedArguments = new Parser().Parse(@"--solution=""C:\Test\My Project.sln""");

        Assert.AreEqual(@"C:\Test\My Project.sln",
            parsedArguments.NamedArguments["--solution"]);
    }

    #endregion

    #region IsSet

    // "IsSet tells you whether it was given at all", against the schema declared under
    // "Declaring your options".
    [TestMethod]
    public void IsSetTellsNotGivenFromGivenTheSameValueAsTheDefault()
    {
        ArgumentSchema schema =
            ArgumentSchema.Create()
                .Option<string>("--output", alias: "-o", required: true, repeatable: true,
                    description: "Where to write the report")
                .Option<int>("--timeout", defaultValue: 60,
                    description: "Seconds before the run is abandoned")
                .Flag("--verbose", alias: "-v")
                .Build();

        Assert.IsFalse(schema.Parse("--output=a.json").IsSet("--timeout"));
        Assert.IsTrue(schema.Parse("--output=a.json --timeout=60").IsSet("--timeout"));
    }

    #endregion

    #region Names are matched without regard to case (schema)

    // "The schema takes the same comparer through WithComparer"
    [TestMethod]
    public void SchemaWithOrdinalComparerReportsTheOtherCasingAsUnknown()
    {
        ArgumentSchema schema =
            ArgumentSchema.Create()
                .WithComparer(StringComparer.Ordinal)
                .Flag("--verbose")
                .Build();

        Assert.IsTrue(schema.Parse("--verbose").Success);
        Assert.AreEqual(ParseErrorKind.UnknownOption,
            schema.Parse("--Verbose").Errors[0].Kind);
    }

    // "The comparer governs enum values too"
    [TestMethod]
    public void SchemaWithOrdinalComparerIsStrictAboutEnumValueCasingToo()
    {
        ArgumentSchema schema =
            ArgumentSchema.Create()
                .WithComparer(StringComparer.Ordinal)
                .Option<EmployeeType>("--type")
                .Build();

        Assert.IsTrue(schema.Parse("--type=Sales").Success);
        Assert.AreEqual(ParseErrorKind.UnconvertibleValue,
            schema.Parse("--type=sales").Errors[0].Kind);
    }

    #endregion

    #region A named argument's key has to carry a prefix

    [TestMethod]
    public void ABareWindowsPathIsAStringArgument()
    {
        var parsedArguments = new Parser().Parse(@"C:\Test\file.txt");

        Assert.AreEqual(0, parsedArguments.NamedArguments.Count);
        Assert.AreEqual(@"C:\Test\file.txt", parsedArguments.StringArguments[0]);
    }

    // "An unprefixed key is a string argument too"
    [TestMethod]
    public void AnUnprefixedKeyIsAStringArgument()
    {
        var parsedArguments = new Parser().Parse("db=MyDb");

        Assert.AreEqual(0, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("db=MyDb", parsedArguments.StringArguments[0]);
    }

    // "Pass an empty array of prefixes to accept any key, which is what 1.x did"
    [TestMethod]
    public void AnEmptyPrefixArrayAcceptsAnyKey()
    {
        var parser = new Parser(namedArgumentPrefixes: new string[0]);

        var parsedArguments = parser.Parse("db=MyDb");

        Assert.AreEqual("MyDb", parsedArguments.NamedArguments["db"]);
    }

    // "or name the prefixes your application actually uses"
    [TestMethod]
    public void WindowsSwitchPrefixIsRecognizedWhenDeclared()
    {
        var parser = new Parser(namedArgumentPrefixes: new[] { "/" });

        var parsedArguments = parser.Parse("/out:a.json");

        Assert.AreEqual("a.json", parsedArguments.NamedArguments["/out"]);
    }

    #endregion

    #region Leading and trailing whitespace is trimmed

    [TestMethod]
    public void QuotedValuesAreTrimmedButKeepTheirInnerSpaces()
    {
        var parsedArguments = new Parser().Parse("--name=\"  a  b  \"");

        Assert.AreEqual("a  b", parsedArguments.NamedArguments["--name"]);
    }

    #endregion
}
