using ArgumentParser;

namespace Tests.ArgumentParser;

/// <summary>
/// Issue #49. Named argument names are matched with StringComparer.OrdinalIgnoreCase by
/// default, which is the convention on Windows where most consumers of this package run, and
/// a caller that wants case-sensitive matching passes StringComparer.Ordinal. The default
/// changed in 2.0.0, which is a breaking change and the reason it waited for a major version.
/// </summary>
[TestClass]
public class TestParserComparer
{
    [TestMethod]
    public void Parse_NoComparer_TreatsNamesThatDifferInCaseAsOneArgument()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse("--output=a --OUTPUT=b");

        Assert.AreEqual(1, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("b", parsedArguments.NamedArguments["--output"]);
    }

    // The 1.x behavior, still available by asking for it.
    [TestMethod]
    public void Parse_OrdinalComparer_TreatsNamesThatDifferInCaseAsSeparateArguments()
    {
        Parser parser = new Parser(comparer: StringComparer.Ordinal);

        ParsedArguments parsedArguments = parser.Parse("--output=a --OUTPUT=b");

        Assert.AreEqual(2, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("a", parsedArguments.NamedArguments["--output"]);
        Assert.AreEqual("b", parsedArguments.NamedArguments["--OUTPUT"]);
    }

    [TestMethod]
    public void Parse_NoComparer_LooksUpUnderAnyCasing()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse("--Output=report.json");

        Assert.AreEqual("report.json", parsedArguments.NamedArguments["--OUTPUT"]);
        Assert.AreEqual("report.json", parsedArguments.NamedArguments["--output"]);
        Assert.IsTrue(parsedArguments.NamedArguments.ContainsKey("--oUtPuT"));
    }

    // Issue #49 noted that a case-insensitive comparer turns "--out=a --OUT=b" into a repeat
    // rather than two names. Issue #39 settled what a repeat does, and AllValuesOf keeps both.
    [TestMethod]
    public void AllValuesOf_NoComparer_CollectsValuesGivenUnderAnyCasing()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse("--out=a --OUT=b --Out=c");

        CollectionAssert.AreEqual(
            new[] { "a", "b", "c" }, parsedArguments.AllValuesOf("--OuT").ToArray());
    }

    [TestMethod]
    public void AllValuesOf_OrdinalComparer_DoesNotMatchADifferentCasing()
    {
        Parser parser = new Parser(comparer: StringComparer.Ordinal);

        ParsedArguments parsedArguments = parser.Parse("--out=a");

        Assert.AreEqual(1, parsedArguments.AllValuesOf("--out").Count);
        Assert.AreEqual(0, parsedArguments.AllValuesOf("--OUT").Count);
    }

    [TestMethod]
    public void Parse_ComparerWithTheStringSeparatorOverload_IsUsedAsWell()
    {
        Parser parser = new Parser(new[] { "--", "-" }, new[] { ':' },
            StringComparer.Ordinal);

        ParsedArguments parsedArguments = parser.Parse("--Solution:a -SOLUTION:b");

        Assert.AreEqual(2, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("a", parsedArguments.NamedArguments["Solution"]);
        Assert.AreEqual("b", parsedArguments.NamedArguments["SOLUTION"]);
    }

    [TestMethod]
    public void FluentParse_WithoutComparer_MatchesNamesCaseInsensitively()
    {
        ParsedArguments parsedArguments =
            FluentArgumentParser
            .Create()
            .Parse("--mode=Fast --MODE=slow");

        Assert.AreEqual(1, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("slow", parsedArguments.NamedArguments["--mode"]);
        Assert.AreEqual(2, parsedArguments.AllValuesOf("--Mode").Count);
    }

    [TestMethod]
    public void FluentParse_WithOrdinalComparer_MatchesNamesCaseSensitively()
    {
        ParsedArguments parsedArguments =
            FluentArgumentParser
            .Create()
            .WithComparer(StringComparer.Ordinal)
            .Parse("--mode=Fast --MODE=slow");

        Assert.AreEqual(2, parsedArguments.NamedArguments.Count);
    }

    // Only the names are compared. Values are never case-folded.
    [TestMethod]
    public void Parse_NoComparer_LeavesValueCasingAlone()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse("--mode=FaSt");

        Assert.AreEqual("FaSt", parsedArguments.NamedArguments["--mode"]);
    }
}
