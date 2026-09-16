using ArgumentParser;

namespace Tests.ArgumentParser;

[TestClass]
public class TestParser
{
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
            parser.Parse("sales:1 --mode:test db=MyDb");

        Assert.AreEqual(3, parsedArguments.Arguments.Count);
        Assert.AreEqual(0, parsedArguments.IntegerArguments.Count);
        Assert.AreEqual(0, parsedArguments.DecimalArguments.Count);
        Assert.AreEqual(0, parsedArguments.StringArguments.Count);
        Assert.AreEqual(0, parsedArguments.EnumArgumentsOfType<EmployeeType>().Count());

        Assert.AreEqual(3, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("1", parsedArguments.NamedArguments["sales"]);
        Assert.AreEqual("test", parsedArguments.NamedArguments["--mode"]);
        Assert.AreEqual("MyDb", parsedArguments.NamedArguments["db"]);
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
