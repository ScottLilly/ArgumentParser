using ArgumentParser;

namespace Tests.ArgumentParser;

[TestClass]
public class TestFluentArgumentParser
{
    [TestMethod]
    public void AddArgumentSeparators_PassedAsAList_SplitsOnEachSeparator()
    {
        ParsedArguments parsedArguments =
            FluentArgumentParser
            .Create()
            .AddArgumentSeparators(new[] { ',', ' ' })
            .Parse("abc,1, 2, 3 4,  ,MARKETING, , , 123.45");

        Assert.AreEqual(7, parsedArguments.Arguments.Count);
        Assert.AreEqual(4, parsedArguments.IntegerArguments.Count);
        Assert.AreEqual(1, parsedArguments.DecimalArguments.Count);
        Assert.AreEqual(2, parsedArguments.StringArguments.Count);
        Assert.AreEqual(1, parsedArguments.EnumArgumentsOfType<EmployeeType>().Count());
    }

    [TestMethod]
    public void AddArgumentSeparator_CalledOncePerSeparator_SplitsOnEachSeparator()
    {
        ParsedArguments parsedArguments =
            FluentArgumentParser
            .Create()
            .AddArgumentSeparator(',')
            .AddArgumentSeparator(' ')
            .Parse("abc,1, 2, 3 4,  ,MARKETING, , , 123.45");

        Assert.AreEqual(7, parsedArguments.Arguments.Count);
        Assert.AreEqual(4, parsedArguments.IntegerArguments.Count);
        Assert.AreEqual(1, parsedArguments.DecimalArguments.Count);
        Assert.AreEqual(2, parsedArguments.StringArguments.Count);
        Assert.AreEqual(1, parsedArguments.EnumArgumentsOfType<EmployeeType>().Count());
    }

    [TestMethod]
    [DataRow(' ', @"--solution c:\App1\App1.sln -s c:\App2\App2.sln", DisplayName = "Space separates key from value")]
    [DataRow(':', @"--solution:c:\App1\App1.sln -s:c:\App2\App2.sln", DisplayName = "Colon separates key from value")]
    public void Parse_MultiCharacterArgumentSeparators_StripsThePrefixFromTheKey(
        char keyValueSeparator, string arguments)
    {
        ParsedArguments parsedArguments =
            FluentArgumentParser
            .Create()
            .AddArgumentSeparator("--")
            .AddArgumentSeparator("-")
            .AddKeyValueSeparator(keyValueSeparator)
            .Parse(arguments);

        Assert.AreEqual(2, parsedArguments.Arguments.Count);
        Assert.AreEqual(@"c:\App1\App1.sln", parsedArguments.NamedArguments["solution"]);
        Assert.AreEqual(@"c:\App2\App2.sln", parsedArguments.NamedArguments["s"]);
    }

    [TestMethod]
    public void AddArgumentSeparator_RepeatingAnExistingSeparator_IsIgnored()
    {
        ParsedArguments parsedArguments =
            FluentArgumentParser
            .Create()
            .AddArgumentSeparators(new[] { "--", "-" })
            .AddArgumentSeparator("-")
            .AddKeyValueSeparators(new[] { ':', '|' })
            .Parse(@"--solution:value1 -s|value2");

        Assert.AreEqual(2, parsedArguments.Arguments.Count);
        Assert.AreEqual("value1", parsedArguments.NamedArguments["solution"]);
        Assert.AreEqual("value2", parsedArguments.NamedArguments["s"]);
    }

    [TestMethod]
    public void Parse_BuilderHeldInAVariable_CanBeCalledSeparatelyFromTheChain()
    {
        IFluentArgumentParserBuilder initializedParser =
            FluentArgumentParser
            .Create()
            .AddArgumentSeparators(new[] { "--", "-" })
            .AddKeyValueSeparators(new[] { ':', '|' });

        ParsedArguments parsedArguments =
            initializedParser.Parse(@"--solution:value1 -s|value2");

        Assert.AreEqual(2, parsedArguments.Arguments.Count);
        Assert.AreEqual("value1", parsedArguments.NamedArguments["solution"]);
        Assert.AreEqual("value2", parsedArguments.NamedArguments["s"]);
    }

    [TestMethod]
    public void Parse_StringArrayAlreadySplitByMain_RejoinsItBeforeParsing()
    {
        string[] args =
        {
            @"--solution",
            @"c:\test.sln",
            @"-s",
            @"c:\test2.sln"
        };

        IFluentArgumentParserBuilder parser =
            FluentArgumentParser.Create()
            .AddArgumentSeparators(new[] { "--", "-" })
            .AddKeyValueSeparator(' ');

        ParsedArguments parsedArguments = parser.Parse(args);

        Assert.AreEqual(0, parsedArguments.IntegerArguments.Count);
        Assert.AreEqual(0, parsedArguments.DecimalArguments.Count);
        Assert.AreEqual(2, parsedArguments.Arguments.Count);
        Assert.AreEqual(2, parsedArguments.NamedArguments.Count);
        Assert.AreEqual(@"c:\test.sln", parsedArguments.NamedArguments["solution"]);
        Assert.AreEqual(@"c:\test2.sln", parsedArguments.NamedArguments["s"]);
    }
}
