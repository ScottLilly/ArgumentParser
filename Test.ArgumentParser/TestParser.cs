using ArgumentParser;

namespace Test.ArgumentParser;

public class TestParser
{
    [Fact]
    public void Test_DefaultSeparators()
    {
        var parser = 
            new Parser();

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
        var parser =
            new Parser(new[] { ',', ' ' });

        var parsedArguments =
            parser.Parse("abc,1, 2, 3,  ,MARKETING, , , 123.45");

        Assert.Equal(6, parsedArguments.Arguments.Count);
        Assert.Equal(3, parsedArguments.IntegerArguments.Count);
        Assert.Single(parsedArguments.DecimalArguments);
        Assert.Equal(2, parsedArguments.StringArguments.Count);
        Assert.Single(parsedArguments.EnumArgumentsOfType<EmployeeType>());
    }
}