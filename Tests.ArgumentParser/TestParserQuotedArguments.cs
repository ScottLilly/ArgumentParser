using ArgumentParser;

namespace Tests.ArgumentParser;

/// <summary>
/// Issue #15. A double quoted section is not split on, so a value can contain an argument
/// separator. The quotes group the value and are removed from it.
/// </summary>
[TestClass]
public class TestParserQuotedArguments
{
    #region Quoting within a single string

    [TestMethod]
    public void Parse_QuotedValueContainingASpace_StaysOneNamedArgument()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments =
            parser.Parse(@"--solution=""C:\Test\My Project.sln""");

        Assert.AreEqual(1, parsedArguments.Arguments.Count);
        Assert.AreEqual(@"C:\Test\My Project.sln",
            parsedArguments.NamedArguments["--solution"]);
    }

    // The same input without quotes, which is what issue #15 reported. Kept alongside so the
    // difference the quotes make is readable in one place.
    [TestMethod]
    public void Parse_UnquotedValueContainingASpace_SplitsIntoSeparateArguments()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments =
            parser.Parse(@"--solution=C:\Test\My Project.sln");

        Assert.AreEqual(2, parsedArguments.Arguments.Count);
        Assert.AreEqual(@"C:\Test\My", parsedArguments.NamedArguments["--solution"]);
        Assert.AreEqual("Project.sln", parsedArguments.StringArguments[0]);
    }

    // Quoting the whole argument works as well as quoting only the value, because the quotes
    // are removed while splitting and the key/value split happens afterwards.
    [TestMethod]
    [DataRow(@"--solution=""C:\My Project.sln""", DisplayName = "Only the value quoted")]
    [DataRow(@"""--solution=C:\My Project.sln""", DisplayName = "Whole argument quoted")]
    public void Parse_QuotesAroundEitherSpan_GiveTheSameResult(string argument)
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse(argument);

        Assert.AreEqual(1, parsedArguments.NamedArguments.Count);
        Assert.AreEqual(@"C:\My Project.sln", parsedArguments.NamedArguments["--solution"]);
    }

    [TestMethod]
    public void Parse_SeveralQuotedArguments_AreSeparatedFromEachOther()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments =
            parser.Parse(@"--in=""a b.txt"" --out=""c d.txt"" plain");

        Assert.AreEqual(3, parsedArguments.Arguments.Count);
        Assert.AreEqual("a b.txt", parsedArguments.NamedArguments["--in"]);
        Assert.AreEqual("c d.txt", parsedArguments.NamedArguments["--out"]);
        Assert.AreEqual("plain", parsedArguments.StringArguments[0]);
    }

    // A quoted value may contain a key/value separator, because the key/value split takes the
    // leftmost separator and the quotes are already gone by then.
    [TestMethod]
    public void Parse_QuotedValueContainingAKeyValueSeparator_KeepsItInTheValue()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse(@"--filter=""name=value""");

        Assert.AreEqual("name=value", parsedArguments.NamedArguments["--filter"]);
    }

    [TestMethod]
    public void Parse_QuotedStringArgument_IsOneStringArgumentWithoutTheQuotes()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse(@"""hello world""");

        Assert.AreEqual(1, parsedArguments.StringArguments.Count);
        Assert.AreEqual("hello world", parsedArguments.StringArguments[0]);
    }

    // An unclosed quote runs to the end of the input rather than being rejected. There is no
    // error reporting in the library yet, so this is the least surprising of the options.
    [TestMethod]
    public void Parse_UnclosedQuote_RunsToTheEndOfTheInput()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse(@"--out=""a b c");

        Assert.AreEqual(1, parsedArguments.Arguments.Count);
        Assert.AreEqual("a b c", parsedArguments.NamedArguments["--out"]);
    }

    // Issue #63. Deliberate, and documented: leading and trailing whitespace is trimmed
    // from a value whether it was quoted or not. Quoting is for the whitespace in the
    // middle of a value, which is left exactly as it was typed.
    [TestMethod]
    public void Parse_QuotedValueWithLeadingAndTrailingSpaces_LosesThemToTheTrim()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse(@"--msg="" a b """);

        Assert.AreEqual("a b", parsedArguments.NamedArguments["--msg"]);
    }

    [TestMethod]
    public void Parse_QuotedValueWithRunsOfSpacesInside_KeepsEveryOneOfThem()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse(@"--msg=""  a   b  """);

        Assert.AreEqual("a   b", parsedArguments.NamedArguments["--msg"]);
    }

    [TestMethod]
    public void Parse_EmptyQuotedValue_GivesAnEmptyValue()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse(@"--out=""""");

        Assert.AreEqual(1, parsedArguments.NamedArguments.Count);
        Assert.AreEqual(string.Empty, parsedArguments.NamedArguments["--out"]);
    }

    // Quotes protect a multi-character separator too, not just whitespace.
    [TestMethod]
    public void Parse_QuotedValueContainingAMultiCharacterSeparator_KeepsItInTheValue()
    {
        Parser parser = new Parser(new[] { "--" }, new[] { '=' });

        ParsedArguments parsedArguments = parser.Parse(@"--range=""a--b""");

        Assert.AreEqual(1, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("a--b", parsedArguments.NamedArguments["range"]);
    }

    #endregion

    #region Quoting restored on the string[] overload

    // The shell removes the quotes before Main sees them, so joining the array back into one
    // string used to split the value again. An element containing whitespace is requoted.
    [TestMethod]
    public void Parse_StringArrayElementContainingASpace_SurvivesAsOneArgument()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments =
            parser.Parse(new[] { @"--solution=C:\Test\My Project.sln" });

        Assert.AreEqual(1, parsedArguments.Arguments.Count);
        Assert.AreEqual(@"C:\Test\My Project.sln",
            parsedArguments.NamedArguments["--solution"]);
    }

    [TestMethod]
    public void Parse_StringArrayWithSpaceAsTheKeyValueSeparator_KeepsTheSpacedValueWhole()
    {
        Parser parser = new Parser(new[] { "--", "-" }, new[] { ' ' });

        ParsedArguments parsedArguments =
            parser.Parse(new[] { "--solution", @"C:\Test\My Project.sln" });

        Assert.AreEqual(1, parsedArguments.NamedArguments.Count);
        Assert.AreEqual(@"C:\Test\My Project.sln",
            parsedArguments.NamedArguments["solution"]);
    }

    // Requoting is driven by whitespace alone. A separator such as "--" is left alone, so the
    // parser can still strip it as a prefix the way it always has.
    [TestMethod]
    public void Parse_StringArrayElementContainingASeparator_IsStillPrefixStripped()
    {
        Parser parser = new Parser(new[] { "--", "-" }, new[] { ' ' });

        ParsedArguments parsedArguments =
            parser.Parse(new[] { "--solution", @"c:\a.sln" });

        Assert.AreEqual(@"c:\a.sln", parsedArguments.NamedArguments["solution"]);
    }

    #endregion

    #region Separator matching

    // The longest separator wins wherever two of them match at the same position, so "--" is
    // preferred over "-" whichever order they were added in.
    [TestMethod]
    [DataRow("--", "-", DisplayName = "Longest added first")]
    [DataRow("-", "--", DisplayName = "Shortest added first")]
    public void Parse_OverlappingSeparators_PreferTheLongestMatch(string first, string second)
    {
        Parser parser = new Parser(new[] { first, second }, new[] { ':' });

        ParsedArguments parsedArguments = parser.Parse("--solution:value1 -s:value2");

        Assert.AreEqual("value1", parsedArguments.NamedArguments["solution"]);
        Assert.AreEqual("value2", parsedArguments.NamedArguments["s"]);
    }

    #endregion
}
