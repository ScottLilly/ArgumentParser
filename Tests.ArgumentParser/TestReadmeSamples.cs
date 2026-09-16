using ArgumentParser;

namespace Tests.ArgumentParser;

/// <summary>
/// Every code sample in README.md, so the documentation cannot drift away from the code
/// again. Issue #45 was the first sample throwing a KeyNotFoundException at anyone who
/// copied it, which nothing caught because nothing ran it.
///
/// Each test is named after the README heading it comes from. A failure here means the
/// README and the code disagree, so fix whichever of the two is wrong rather than only
/// the test. The samples are written with xUnit assertions, since that is what the README
/// shows, so they are translated to MSTest here but otherwise left alone.
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
}
