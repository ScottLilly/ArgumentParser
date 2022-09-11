using ArgumentParser;

namespace Test.ArgumentParser;

public class TestParser
{
    [Fact]
    public void Test_DefaultSeparators()
    {
        var parser = 
            new Parser("sales 1");

        Assert.Equal(2, parser.Arguments.Count());
        Assert.Single(parser.IntegerArguments);
        Assert.Empty(parser.DecimalArguments);
        Assert.Single(parser.StringArguments);
    }

    [Fact]
    public void Test_PassedInSeparators()
    {
        var parser = 
            new Parser("sales,abc,1, 2, 3,  , , , 123.45", 
                new []{',', ' '});

        Assert.Equal(6, parser.Arguments.Count());
        Assert.Equal(3, parser.IntegerArguments.Count());
        Assert.Single(parser.DecimalArguments);
        Assert.Equal(2, parser.StringArguments.Count());
    }
}