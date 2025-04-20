using ArgumentParser;

namespace Test.ArgumentParser
{
    public class TestFluentArgumentParser
    {
        // Used for testing enum parsing
        public enum EmployeeType
        {
            Production,
            Sales,
            Marketing,
        }

        [Fact]
        public void Test_PassedInSeparatorsList()
        {
            ParsedArguments parsedArguments = 
                FluentArgumentParser
                .Create()
                .AddArgumentSeparators(new[] { ',', ' ' })
                .Parse("abc,1, 2, 3 4,  ,MARKETING, , , 123.45");

            Assert.Equal(7, parsedArguments.Arguments.Count);
            Assert.Equal(4, parsedArguments.IntegerArguments.Count);
            Assert.Single(parsedArguments.DecimalArguments);
            Assert.Equal(2, parsedArguments.StringArguments.Count);
            Assert.Single(parsedArguments.EnumArgumentsOfType<EmployeeType>());
        }

        [Fact]
        public void Test_PassedInSeparatorsIndividually()
        {
            ParsedArguments parsedArguments =
                FluentArgumentParser
                .Create()
                .AddArgumentSeparator(',')
                .AddArgumentSeparator(' ')
                .Parse("abc,1, 2, 3 4,  ,MARKETING, , , 123.45");

            Assert.Equal(7, parsedArguments.Arguments.Count);
            Assert.Equal(4, parsedArguments.IntegerArguments.Count);
            Assert.Single(parsedArguments.DecimalArguments);
            Assert.Equal(2, parsedArguments.StringArguments.Count);
            Assert.Single(parsedArguments.EnumArgumentsOfType<EmployeeType>());
        }

        [Fact]
        public void Test_NamedArgumentsWithStringSeparators()
        {
            ParsedArguments parsedArguments =
                FluentArgumentParser
                .Create()
                .AddArgumentSeparator("--")
                .AddArgumentSeparator("-")
                .AddKeyValueSeparator(' ')
                .Parse(@"--solution c:\App1\App1.sln -s c:\App2\App2.sln");

            Assert.Equal(2, parsedArguments.Arguments.Count);
            Assert.Equal(@"c:\App1\App1.sln", parsedArguments.NamedArguments["solution"]);
            Assert.Equal(@"c:\App2\App2.sln", parsedArguments.NamedArguments["s"]);
        }

        [Fact]
        public void Test_NamedArgumentsWithStringSeparators2()
        {
            ParsedArguments parsedArguments =
                FluentArgumentParser
                .Create()
                .AddArgumentSeparator("--")
                .AddArgumentSeparator("-")
                .AddKeyValueSeparator(':')
                .Parse(@"--solution:c:\App1\App1.sln -s:c:\App2\App2.sln");

            Assert.Equal(2, parsedArguments.Arguments.Count);
            Assert.Equal(@"c:\App1\App1.sln", parsedArguments.NamedArguments["solution"]);
            Assert.Equal(@"c:\App2\App2.sln", parsedArguments.NamedArguments["s"]);
        }

        [Fact]
        public void Test_NamedArgumentsWithStringSeparators3()
        {
            ParsedArguments parsedArguments =
                FluentArgumentParser
                .Create()
                .AddArgumentSeparators(new string[] { "--", "-" })
                .AddArgumentSeparator("-") // Should be ignored since we already have it in the list
                .AddKeyValueSeparators(new char[] { ':', '|' })
                .Parse(@"--solution:value1 -s|value2");

            Assert.Equal(2, parsedArguments.Arguments.Count);
            Assert.Equal("value1", parsedArguments.NamedArguments["solution"]);
            Assert.Equal("value2", parsedArguments.NamedArguments["s"]);
        }

        [Fact]
        public void Test_SplitFluentInterface()
        {
            var initializedParser = 
                FluentArgumentParser
                .Create()
                .AddArgumentSeparators(new string[] { "--", "-" })
                .AddArgumentSeparator("-") // Should be ignored since we already have it in the list
                .AddKeyValueSeparators(new char[] { ':', '|' });

            ParsedArguments parsedArguments =
                initializedParser.Parse(@"--solution:value1 -s|value2");

            Assert.Equal(2, parsedArguments.Arguments.Count);
            Assert.Equal("value1", parsedArguments.NamedArguments["solution"]);
            Assert.Equal("value2", parsedArguments.NamedArguments["s"]);
        }
    }
}
