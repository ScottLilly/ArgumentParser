using ArgumentParser;

namespace Tests.ArgumentParser;

[TestClass]
public class TestParser
{
    // Issue #66. Parser was the last public type nothing could usefully derive from that
    // was still open. ArgumentParseException stays unsealed, which is normal for an
    // exception type.
    [TestMethod]
    public void Parser_IsSealed()
    {
        Assert.IsTrue(typeof(Parser).IsSealed);
    }

    [TestMethod]
    public void Parse_DefaultSeparators_SplitsOnSpace()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse("sales 1");

        Assert.AreEqual(2, parsedArguments.Arguments.Count);
        Assert.AreEqual(1, parsedArguments.IntegerArguments.Count);
        Assert.AreEqual(0, parsedArguments.DecimalArguments.Count);
        Assert.AreEqual(1, parsedArguments.StringArguments.Count);
        Assert.AreEqual(1, parsedArguments.EnumArgumentsOfType<EmployeeType>().Count());
    }

    [TestMethod]
    public void Parse_PassedInSeparators_SplitsOnEachSeparatorAndDropsEmptyEntries()
    {
        Parser parser = new Parser(new[] { ',', ' ' });

        ParsedArguments parsedArguments =
            parser.Parse("abc,1, 2, 3 4,  ,MARKETING, , , 123.45");

        Assert.AreEqual(7, parsedArguments.Arguments.Count);
        Assert.AreEqual(4, parsedArguments.IntegerArguments.Count);
        Assert.AreEqual(1, parsedArguments.DecimalArguments.Count);
        Assert.AreEqual(2, parsedArguments.StringArguments.Count);
        Assert.AreEqual(1, parsedArguments.EnumArgumentsOfType<EmployeeType>().Count());
    }

    [TestMethod]
    [DataRow("123", 1, 0, 0, DisplayName = "Integer")]
    [DataRow("-5", 1, 0, 0, DisplayName = "Negative integer")]
    [DataRow("45.67", 0, 1, 0, DisplayName = "Decimal")]
    [DataRow("-45.67", 0, 1, 0, DisplayName = "Negative decimal")]
    [DataRow("hello", 0, 0, 1, DisplayName = "Word")]
    [DataRow("12a", 0, 0, 1, DisplayName = "Digits followed by a letter")]
    [DataRow(@"c_temp", 0, 0, 1, DisplayName = "Word containing no key/value separator")]
    public void Parse_SingleArgument_ClassifiesItByType(
        string argument, int expectedIntegers, int expectedDecimals, int expectedStrings)
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse(argument);

        Assert.AreEqual(1, parsedArguments.Arguments.Count);
        Assert.AreEqual(expectedIntegers, parsedArguments.IntegerArguments.Count);
        Assert.AreEqual(expectedDecimals, parsedArguments.DecimalArguments.Count);
        Assert.AreEqual(expectedStrings, parsedArguments.StringArguments.Count);
    }

    [TestMethod]
    public void Parse_NamedArguments_AcceptsEitherDefaultKeyValueSeparator()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments =
            parser.Parse("-sales:1 --mode:test --db=MyDb");

        Assert.AreEqual(3, parsedArguments.Arguments.Count);
        Assert.AreEqual(0, parsedArguments.IntegerArguments.Count);
        Assert.AreEqual(0, parsedArguments.DecimalArguments.Count);
        Assert.AreEqual(0, parsedArguments.StringArguments.Count);
        Assert.AreEqual(0, parsedArguments.EnumArgumentsOfType<EmployeeType>().Count());

        Assert.AreEqual(3, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("1", parsedArguments.NamedArguments["-sales"]);
        Assert.AreEqual("test", parsedArguments.NamedArguments["--mode"]);
        Assert.AreEqual("MyDb", parsedArguments.NamedArguments["--db"]);
    }

    // Issue #58: the key has to carry a prefix, so a key/value separator inside something
    // that was never a named argument does not turn it into one.
    [TestMethod]
    public void Parse_UnprefixedKeys_AreStringArgumentsRatherThanNamedArguments()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments =
            parser.Parse(@"sales:1 db=MyDb C:\Test\file.txt");

        Assert.AreEqual(0, parsedArguments.NamedArguments.Count);
        CollectionAssert.AreEqual(
            new[] { "sales:1", "db=MyDb", @"C:\Test\file.txt" },
            parsedArguments.StringArguments.ToArray());
    }

    [TestMethod]
    public void Parse_EmptyNamedArgumentPrefixes_AcceptsAnUnprefixedKey()
    {
        Parser parser = new Parser(namedArgumentPrefixes: new string[0]);

        ParsedArguments parsedArguments = parser.Parse("sales:1 db=MyDb");

        Assert.AreEqual(2, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("1", parsedArguments.NamedArguments["sales"]);
        Assert.AreEqual("MyDb", parsedArguments.NamedArguments["db"]);
    }

    [TestMethod]
    public void Parse_ConfiguredNamedArgumentPrefixes_ReplaceTheDefaults()
    {
        Parser parser = new Parser(namedArgumentPrefixes: new[] { "/" });

        ParsedArguments parsedArguments = parser.Parse("/out:a.json --mode:test");

        Assert.AreEqual(1, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("a.json", parsedArguments.NamedArguments["/out"]);
        Assert.AreEqual("--mode:test", parsedArguments.StringArguments[0]);
    }

    // A prefix that is also an argument separator has been eaten by the split, so the key
    // arrives without it. The token still followed the prefix, which is what counts.
    [TestMethod]
    public void Parse_PrefixUsedAsAnArgumentSeparator_StillCountsAsPrefixed()
    {
        Parser parser = new Parser(new[] { "--", "-" }, new[] { ':' });

        ParsedArguments parsedArguments = parser.Parse("--solution:a -s:b");

        Assert.AreEqual(2, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("a", parsedArguments.NamedArguments["solution"]);
        Assert.AreEqual("b", parsedArguments.NamedArguments["s"]);
    }

    // The same configuration, on an argument that never followed a prefix. A leading
    // separator would have produced an empty token, so there is nothing to mistake here.
    [TestMethod]
    public void Parse_PrefixUsedAsAnArgumentSeparator_DoesNotCoverAnUnprefixedArgument()
    {
        Parser parser = new Parser(new[] { "--", "-" }, new[] { ':' });

        ParsedArguments parsedArguments = parser.Parse("solution:a");

        Assert.AreEqual(0, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("solution:a", parsedArguments.StringArguments[0]);
    }

    [TestMethod]
    public void EnumArgumentsOfType_ArgumentsMatchingEnumNames_ReturnsOneValuePerMatch()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse("production sales marketing");

        Assert.AreEqual(3, parsedArguments.Arguments.Count);
        Assert.AreEqual(0, parsedArguments.IntegerArguments.Count);
        Assert.AreEqual(0, parsedArguments.DecimalArguments.Count);
        Assert.AreEqual(3, parsedArguments.StringArguments.Count);

        CollectionAssert.AreEqual(
            new[] { EmployeeType.Production, EmployeeType.Sales, EmployeeType.Marketing },
            parsedArguments.EnumArgumentsOfType<EmployeeType>().ToArray());
    }

    [TestMethod]
    [DataRow(' ', @"--solution c:\App1\App1.sln -s c:\App2\App2.sln", DisplayName = "Space separates key from value")]
    [DataRow(':', @"--solution:c:\App1\App1.sln -s:c:\App2\App2.sln", DisplayName = "Colon separates key from value")]
    public void Parse_MultiCharacterArgumentSeparators_StripsThePrefixFromTheKey(
        char keyValueSeparator, string arguments)
    {
        Parser parser = new Parser(new[] { "--", "-" }, new[] { keyValueSeparator });

        ParsedArguments parsedArguments = parser.Parse(arguments);

        Assert.AreEqual(2, parsedArguments.Arguments.Count);
        Assert.AreEqual(@"c:\App1\App1.sln", parsedArguments.NamedArguments["solution"]);
        Assert.AreEqual(@"c:\App2\App2.sln", parsedArguments.NamedArguments["s"]);
    }

    [TestMethod]
    public void Parse_MixtureOfArgumentTypes_PopulatesEveryCollection()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse("123 45.67 hello world --key=value");

        Assert.AreEqual(5, parsedArguments.Arguments.Count);
        Assert.AreEqual(1, parsedArguments.IntegerArguments.Count);
        Assert.AreEqual(1, parsedArguments.DecimalArguments.Count);
        Assert.AreEqual(2, parsedArguments.StringArguments.Count);
        Assert.AreEqual(1, parsedArguments.NamedArguments.Count);
    }

    [TestMethod]
    public void Parse_KeyValueSeparatorInFirstPosition_IsNotANamedArgument()
    {
        Parser parser = new Parser(new[] { ' ' }, new[] { '=' });

        ParsedArguments parsedArguments = parser.Parse("=1");

        Assert.AreEqual(1, parsedArguments.Arguments.Count);
        Assert.AreEqual(0, parsedArguments.IntegerArguments.Count);
        Assert.AreEqual(0, parsedArguments.DecimalArguments.Count);
        Assert.AreEqual(1, parsedArguments.StringArguments.Count);
        Assert.AreEqual(0, parsedArguments.EnumArgumentsOfType<EmployeeType>().Count());
        Assert.AreEqual(0, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("=1", parsedArguments.Arguments[0]);
    }
}
