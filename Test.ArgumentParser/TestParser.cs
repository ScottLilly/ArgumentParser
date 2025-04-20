using ArgumentParser;

namespace Test.ArgumentParser;

public class TestParser
{
    // Used for testing enum parsing
    public enum EmployeeType
    {
        Production,
        Sales,
        Marketing,
    }

    [Fact]
    public void Test_DefaultSeparators()
    {
        var parser = new Parser();

        var parsedArguments =
            parser.Parse("sales 1");

        Assert.Equal(2, parsedArguments.Arguments.Count);
        Assert.Single(parsedArguments.IntegerArguments);
        Assert.Empty(parsedArguments.DecimalArguments);
        Assert.Single(parsedArguments.StringArguments);
        Assert.Single(parsedArguments.EnumArgumentsOfType<EmployeeType>());
    }

    [Fact]
    public void Test_PassedInSeparators()
    {
        var parser = new Parser(new[] { ',', ' ' });

        var parsedArguments =
            parser.Parse("abc,1, 2, 3 4,  ,MARKETING, , , 123.45");

        Assert.Equal(7, parsedArguments.Arguments.Count);
        Assert.Equal(4, parsedArguments.IntegerArguments.Count);
        Assert.Single(parsedArguments.DecimalArguments);
        Assert.Equal(2, parsedArguments.StringArguments.Count);
        Assert.Single(parsedArguments.EnumArgumentsOfType<EmployeeType>());
    }

    [Fact]
    public void Test_NamedArguments()
    {
        var parser = new Parser();

        var parsedArguments =
            parser.Parse("sales:1 --mode:test db=MyDb");

        Assert.Equal(3, parsedArguments.Arguments.Count);
        Assert.Empty(parsedArguments.IntegerArguments);
        Assert.Empty(parsedArguments.DecimalArguments);
        Assert.Empty(parsedArguments.StringArguments);
        Assert.Empty(parsedArguments.EnumArgumentsOfType<EmployeeType>());

        Assert.Equal(3, parsedArguments.NamedArguments.Count);
        Assert.Equal("1", parsedArguments.NamedArguments["sales"]);
        Assert.Equal("test", parsedArguments.NamedArguments["--mode"]);
        Assert.Equal("MyDb", parsedArguments.NamedArguments["db"]);
    }

    [Fact]
    public void Test_EnumParsing()
    {
        var parser = new Parser();

        var parsedArguments =
            parser.Parse("production sales marketing");

        Assert.Equal(3, parsedArguments.Arguments.Count);
        Assert.Empty(parsedArguments.IntegerArguments);
        Assert.Empty(parsedArguments.DecimalArguments);
        Assert.Equal(3, parsedArguments.StringArguments.Count);
        Assert.Equal(3, parsedArguments.EnumArgumentsOfType<EmployeeType>().Count());
    }

    [Fact]
    public void Test_NamedArgumentsWithStringSeparators()
    {
        var parser = new Parser(new string[] { "--", "-" }, new char[] { ' ' });

        var parsedArguments =
            parser.Parse(@"--solution c:\App1\App1.sln -s c:\App2\App2.sln");

        Assert.Equal(2, parsedArguments.Arguments.Count);
    }

    [Fact]
    public void Test_ArgumentVariety()
    {
        var parser = new Parser();

        var parsedArguments = parser.Parse("123 45.67 hello world --key=value");

        Assert.Equal(5, parsedArguments.Arguments.Count);
        Assert.Equal(1, parsedArguments.IntegerArguments.Count);
        Assert.Equal(1, parsedArguments.DecimalArguments.Count);
        Assert.Equal(2, parsedArguments.StringArguments.Count);
        Assert.Equal(1, parsedArguments.NamedArguments.Count);
    }

    [Fact]
    public void Test_KeyValueSeparatorInFirstPosition()
    {
        var parser = new Parser(new[] { ' ' }, new[] { '=' });

        var parsedArguments = parser.Parse("=1");

        Assert.Single(parsedArguments.Arguments);
        Assert.Empty(parsedArguments.IntegerArguments);
        Assert.Empty(parsedArguments.DecimalArguments);
        Assert.Single(parsedArguments.StringArguments);
        Assert.Empty(parsedArguments.EnumArgumentsOfType<EmployeeType>());
        Assert.Empty(parsedArguments.NamedArguments);
        Assert.Equal("=1", parsedArguments.Arguments[0]);
    }
}