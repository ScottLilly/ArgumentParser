using System.Globalization;

using ArgumentParser;

namespace Tests.ArgumentParser;

/// <summary>
/// The edge cases listed in issue #51. These started out pinning behavior that was known to
/// be wrong, so that fixing the issue behind each one could not pass unnoticed: the test
/// failed and was rewritten to assert the corrected behavior. That has now happened to all
/// of them, for issues #39, #40, #41, #42, #43 and #44.
/// </summary>
[TestClass]
public class TestParserEdgeCases
{
    #region Missing and empty input

    // Issue #42: null means no arguments rather than being an error, so a Main(string[] args)
    // caller does not have to guard the call. This matches what empty input already did.
    [TestMethod]
    public void Parse_NullString_ReturnsNoArguments()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse((string?)null);

        AssertNothingWasParsed(parsedArguments);
    }

    [TestMethod]
    public void Parse_NullStringArray_ReturnsNoArguments()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse((string?[]?)null);

        AssertNothingWasParsed(parsedArguments);
    }

    // A null element is not the same as a null array. It contributes nothing and the
    // arguments either side of it still parse.
    [TestMethod]
    public void Parse_StringArrayContainingNullElements_IgnoresThem()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments =
            parser.Parse(new[] { "--mode=fast", null, "7", null });

        Assert.AreEqual(2, parsedArguments.Arguments.Count);
        Assert.AreEqual("fast", parsedArguments.NamedArguments["--mode"]);
        Assert.AreEqual(7, parsedArguments.IntegerArguments[0]);
    }

    // The fluent path goes through the same Parse, so it gets the same contract.
    [TestMethod]
    public void FluentParse_NullString_ReturnsNoArguments()
    {
        ParsedArguments parsedArguments =
            FluentArgumentParser.Create().Parse((string?)null);

        AssertNothingWasParsed(parsedArguments);
    }

    [TestMethod]
    public void FluentParse_NullStringArray_ReturnsNoArguments()
    {
        ParsedArguments parsedArguments =
            FluentArgumentParser.Create().Parse((string?[]?)null);

        AssertNothingWasParsed(parsedArguments);
    }

    [TestMethod]
    public void Parse_EmptyString_ReturnsNoArguments()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse(string.Empty);

        AssertNothingWasParsed(parsedArguments);
    }

    [TestMethod]
    public void Parse_WhitespaceOnlyString_ReturnsNoArguments()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse("   ");

        AssertNothingWasParsed(parsedArguments);
    }

    [TestMethod]
    public void Parse_EmptyStringArray_ReturnsNoArguments()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse(Array.Empty<string>());

        AssertNothingWasParsed(parsedArguments);
    }

    #endregion

    #region Repeated keys

    // Issue #39: NamedArguments keeps the last value, which is what most command line
    // applications do with a repeated option, so this is now the deliberate contract rather
    // than an accident. AllValuesOf is what stops the earlier value being lost.
    [TestMethod]
    public void Parse_KeyGivenMoreThanOnce_KeepsOnlyTheLastValue()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse("--mode:a --mode:b");

        Assert.AreEqual(2, parsedArguments.Arguments.Count);
        Assert.AreEqual(1, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("b", parsedArguments.NamedArguments["--mode"]);
    }

    [TestMethod]
    public void AllValuesOf_KeyGivenMoreThanOnce_ReturnsEveryValueInTheOrderGiven()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments =
            parser.Parse("--output=a.json --output=b.sarif --output=c.xml");

        CollectionAssert.AreEqual(
            new[] { "a.json", "b.sarif", "c.xml" },
            parsedArguments.AllValuesOf("--output").ToArray());
    }

    // The two views never disagree about which names are present, or about the winner.
    [TestMethod]
    public void AllValuesOf_AndNamedArguments_AgreeOnTheLastValue()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse("--mode:a --mode:b --other:x");

        foreach (KeyValuePair<string, string> named in parsedArguments.NamedArguments)
        {
            IReadOnlyList<string> allValues = parsedArguments.AllValuesOf(named.Key);

            Assert.AreEqual(named.Value, allValues[allValues.Count - 1]);
        }

        Assert.AreEqual(2, parsedArguments.NamedArguments.Count);
    }

    [TestMethod]
    public void AllValuesOf_KeyGivenOnce_ReturnsThatOneValue()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse("--mode:a");

        CollectionAssert.AreEqual(
            new[] { "a" }, parsedArguments.AllValuesOf("--mode").ToArray());
    }

    // Repeating a name with the same value is still two values. Collapsing them would be a
    // judgment about intent that belongs with the declared schema in issue #46.
    [TestMethod]
    public void AllValuesOf_SameValueGivenTwice_ReturnsItTwice()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse("--exclude=bin --exclude=bin");

        Assert.AreEqual(2, parsedArguments.AllValuesOf("--exclude").Count);
    }

    [TestMethod]
    [DataRow("--notGiven", DisplayName = "Name that was not given")]
    [DataRow("", DisplayName = "Empty name")]
    [DataRow(null, DisplayName = "Null name")]
    public void AllValuesOf_NameThatIsNotPresent_ReturnsAnEmptyList(string? key)
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse("--mode:a");

        Assert.AreEqual(0, parsedArguments.AllValuesOf(key).Count);
    }

    // The fluent path builds the same ParsedArguments, so it carries the same values.
    [TestMethod]
    public void FluentParse_KeyGivenMoreThanOnce_KeepsEveryValue()
    {
        ParsedArguments parsedArguments =
            FluentArgumentParser.Create().Parse("--exclude=bin --exclude=obj");

        Assert.AreEqual("obj", parsedArguments.NamedArguments["--exclude"]);
        CollectionAssert.AreEqual(
            new[] { "bin", "obj" },
            parsedArguments.AllValuesOf("--exclude").ToArray());
    }

    #endregion

    #region Separator characters inside a value

    // Issue #40: matching is leftmost-wins, so the separator the caller typed is the one that
    // splits the argument, whatever order the separators were configured in. Everything after
    // it is the value, including further separator characters.
    [TestMethod]
    [DataRow(@"--out=C:\build", "--out", @"C:\build", DisplayName = "Drive letter colon in a Windows path")]
    [DataRow("--url=http://x", "--url", "http://x", DisplayName = "Scheme colon in a URL")]
    [DataRow("--output=json:a.json", "--output", "json:a.json", DisplayName = "format:path style value")]
    [DataRow("--mode:a=b", "--mode", "a=b", DisplayName = "Equals sign inside a colon-separated value")]
    public void Parse_ValueContainingAnotherSeparator_SplitsAtTheLeftmostSeparator(
        string argument, string expectedKey, string expectedValue)
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse(argument);

        Assert.AreEqual(1, parsedArguments.NamedArguments.Count);
        Assert.AreEqual(expectedValue, parsedArguments.NamedArguments[expectedKey]);
    }

    // The same argument with '=' as the only key/value separator, which is how this was
    // written before issue #40 was fixed and is the behavior the default separators now match.
    [TestMethod]
    public void Parse_ValueContainingAColon_SplitsCorrectlyWhenColonIsNotASeparator()
    {
        Parser parser = new Parser(new[] { ' ' }, new[] { '=' });

        ParsedArguments parsedArguments = parser.Parse(@"--out=C:\build");

        Assert.AreEqual(1, parsedArguments.NamedArguments.Count);
        Assert.AreEqual(@"C:\build", parsedArguments.NamedArguments["--out"]);
    }

    #endregion

    #region Culture

    // Issue #41: numbers are classified with the invariant culture, so a command line means
    // the same thing on a developer machine and on a CI agent. CurrentCulture is set here
    // rather than relying on the test machine's own, so this fails everywhere or nowhere.
    [TestMethod]
    [DataRow("de-DE", DisplayName = "Comma decimal separator, period group separator")]
    [DataRow("en-US", DisplayName = "Period decimal separator, comma group separator")]
    [DataRow("fr-FR", DisplayName = "Comma decimal separator, narrow space group separator")]
    public void Parse_PointDecimal_ReadsThePointAsADecimalPointWhateverTheCurrentCulture(
        string cultureName)
    {
        RunUnderCulture(cultureName, () =>
        {
            ParsedArguments parsedArguments = new Parser().Parse("123.45");

            Assert.AreEqual(1, parsedArguments.DecimalArguments.Count);
            Assert.AreEqual(123.45m, parsedArguments.DecimalArguments[0]);
            Assert.AreEqual(0, parsedArguments.StringArguments.Count);
        });
    }

    // A German-style decimal is not a number under the invariant culture, and a comma is not
    // read as a group separator either, so it is a string argument on every machine.
    [TestMethod]
    [DataRow("de-DE", DisplayName = "German culture")]
    [DataRow("en-US", DisplayName = "US culture")]
    public void Parse_CommaDecimal_IsAStringArgumentWhateverTheCurrentCulture(
        string cultureName)
    {
        RunUnderCulture(cultureName, () =>
        {
            ParsedArguments parsedArguments = new Parser().Parse("123,45");

            Assert.AreEqual(0, parsedArguments.DecimalArguments.Count);
            Assert.AreEqual(0, parsedArguments.IntegerArguments.Count);
            Assert.AreEqual(1, parsedArguments.StringArguments.Count);
            Assert.AreEqual("123,45", parsedArguments.StringArguments[0]);
        });
    }

    // Issue #41: the tokens a command line never legitimately carries. Group separators, a
    // trailing sign and an exponent are all rejected, so a token that only resembles a number
    // stays a string argument rather than disappearing out of StringArguments.
    [TestMethod]
    [DataRow("1,234", DisplayName = "Group separator")]
    [DataRow("5-", DisplayName = "Trailing sign")]
    [DataRow("1e5", DisplayName = "Exponent")]
    [DataRow("(5)", DisplayName = "Accounting negative")]
    [DataRow("1 234", DisplayName = "Space as a group separator")]
    public void Parse_TokenThatOnlyResemblesANumber_IsAStringArgument(string argument)
    {
        ParsedArguments parsedArguments = new Parser(new[] { '\t' }).Parse(argument);

        Assert.AreEqual(0, parsedArguments.IntegerArguments.Count);
        Assert.AreEqual(0, parsedArguments.DecimalArguments.Count);
        Assert.AreEqual(argument, parsedArguments.StringArguments[0]);
    }

    // The forms that are accepted, pinned alongside the rejections above so the boundary is
    // readable in one place.
    [TestMethod]
    [DataRow("123", 123, DisplayName = "Digits")]
    [DataRow("-5", -5, DisplayName = "Leading minus")]
    [DataRow("+5", 5, DisplayName = "Leading plus")]
    public void Parse_AcceptedIntegerForms_AreIntegerArguments(string argument, int expected)
    {
        ParsedArguments parsedArguments = new Parser().Parse(argument);

        Assert.AreEqual(1, parsedArguments.IntegerArguments.Count);
        Assert.AreEqual(expected, parsedArguments.IntegerArguments[0]);
    }

    [TestMethod]
    [DataRow("45.67", 45.67, DisplayName = "Digits either side of the point")]
    [DataRow("-45.67", -45.67, DisplayName = "Leading minus")]
    [DataRow(".5", 0.5, DisplayName = "No leading digit")]
    [DataRow("45.", 45.0, DisplayName = "No trailing digit")]
    public void Parse_AcceptedDecimalForms_AreDecimalArguments(string argument, double expected)
    {
        ParsedArguments parsedArguments = new Parser().Parse(argument);

        Assert.AreEqual(1, parsedArguments.DecimalArguments.Count);
        Assert.AreEqual((decimal)expected, parsedArguments.DecimalArguments[0]);
    }

    // Integers are read with NumberStyles.Integer, which allows no group separators at all.
    [TestMethod]
    [DataRow("de-DE", DisplayName = "German culture")]
    [DataRow("en-US", DisplayName = "US culture")]
    public void Parse_PlainInteger_IsAnIntegerWhateverTheCurrentCulture(string cultureName)
    {
        RunUnderCulture(cultureName, () =>
        {
            ParsedArguments parsedArguments = new Parser().Parse("1234 -5");

            CollectionAssert.AreEqual(
                new[] { 1234, -5 }, parsedArguments.IntegerArguments.ToArray());
        });
    }

    private static void RunUnderCulture(string cultureName, Action test)
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultureName);

            test();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    #endregion

    #region Fluent parser with nothing configured

    // Issue #43: a fluent builder with nothing added falls back to the same defaults the
    // plain constructor uses, so Create().Parse(...) and new Parser().Parse(...) agree.
    [TestMethod]
    public void FluentParse_NoSeparatorsConfigured_UsesTheDefaultSeparators()
    {
        ParsedArguments parsedArguments =
            FluentArgumentParser.Create().Parse("--mode:test extra 1");

        Assert.AreEqual(3, parsedArguments.Arguments.Count);
        Assert.AreEqual(1, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("test", parsedArguments.NamedArguments["--mode"]);
        Assert.AreEqual(1, parsedArguments.StringArguments.Count);
        Assert.AreEqual(1, parsedArguments.IntegerArguments.Count);
    }

    [TestMethod]
    public void Parse_NoSeparatorsPassedToTheConstructor_UsesTheDefaultSeparators()
    {
        ParsedArguments parsedArguments = new Parser().Parse("--mode:test extra 1");

        Assert.AreEqual(1, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("test", parsedArguments.NamedArguments["--mode"]);
    }

    // Only the half that was configured falls back. Adding a key/value separator and no
    // argument separator still splits on the default space.
    [TestMethod]
    public void FluentParse_OnlyKeyValueSeparatorConfigured_StillSplitsOnTheDefaultSpace()
    {
        ParsedArguments parsedArguments =
            FluentArgumentParser
            .Create()
            .AddKeyValueSeparator('|')
            .Parse("--mode|test extra");

        Assert.AreEqual(2, parsedArguments.Arguments.Count);
        Assert.AreEqual("test", parsedArguments.NamedArguments["--mode"]);
        Assert.AreEqual(1, parsedArguments.StringArguments.Count);
    }

    // The mirror of the above: an argument separator with no key/value separator still uses
    // the default ':' and '=' rather than matching nothing.
    [TestMethod]
    public void FluentParse_OnlyArgumentSeparatorConfigured_StillUsesTheDefaultKeyValueSeparators()
    {
        ParsedArguments parsedArguments =
            FluentArgumentParser
            .Create()
            .AddArgumentSeparator(',')
            .Parse("--mode=test,--other:value");

        Assert.AreEqual(2, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("test", parsedArguments.NamedArguments["--mode"]);
        Assert.AreEqual("value", parsedArguments.NamedArguments["--other"]);
    }

    // Issue #58: the same arguments without prefixes, which the fluent builder accepts
    // again once the prefix requirement is turned off.
    [TestMethod]
    public void FluentParse_EmptyNamedArgumentPrefixes_AcceptsUnprefixedKeys()
    {
        ParsedArguments parsedArguments =
            FluentArgumentParser
            .Create()
            .AddArgumentSeparator(',')
            .WithNamedArgumentPrefixes(new string[0])
            .Parse("mode=test,other:value");

        Assert.AreEqual(2, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("test", parsedArguments.NamedArguments["mode"]);
        Assert.AreEqual("value", parsedArguments.NamedArguments["other"]);
    }

    [TestMethod]
    public void FluentParse_NullNamedArgumentPrefixes_KeepsTheDefaults()
    {
        ParsedArguments parsedArguments =
            FluentArgumentParser
            .Create()
            .WithNamedArgumentPrefixes(null)
            .Parse("--mode=test other=value");

        Assert.AreEqual(1, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("test", parsedArguments.NamedArguments["--mode"]);
        Assert.AreEqual("other=value", parsedArguments.StringArguments[0]);
    }

    #endregion

    #region EnumArgumentsOfType

    // Issue #44: the constraint is now "struct, Enum", so EnumArgumentsOfType<int>() and
    // EnumArgumentsOfType<DateTime>() are compile errors rather than run time ArgumentExceptions.
    // That cannot be asserted from a test, so what is pinned here is that the enum case still
    // works and that a non-matching string is skipped rather than throwing.
    [TestMethod]
    public void EnumArgumentsOfType_ArgumentsThatDoNotMatchAnyName_AreSkipped()
    {
        ParsedArguments parsedArguments = new Parser().Parse("production nonsense sales");

        CollectionAssert.AreEqual(
            new[] { EmployeeType.Production, EmployeeType.Sales },
            parsedArguments.EnumArgumentsOfType<EmployeeType>().ToArray());
    }

    #endregion

    #region Arguments with no value

    // A flag with no value is not a named argument, so a caller has to look for it in
    // StringArguments. Issues #46 and #47 cover declaring and validating options.
    [TestMethod]
    public void Parse_ArgumentWithNoValue_IsAStringArgumentRatherThanANamedArgument()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse("--offline");

        Assert.AreEqual(1, parsedArguments.Arguments.Count);
        Assert.AreEqual(0, parsedArguments.NamedArguments.Count);
        Assert.AreEqual(1, parsedArguments.StringArguments.Count);
        Assert.AreEqual("--offline", parsedArguments.StringArguments[0]);
    }

    [TestMethod]
    public void Parse_ValuelessAndNamedArgumentsTogether_SeparatesThem()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse("--offline --mode=fast");

        Assert.AreEqual(2, parsedArguments.Arguments.Count);
        Assert.AreEqual(1, parsedArguments.StringArguments.Count);
        Assert.AreEqual("--offline", parsedArguments.StringArguments[0]);
        Assert.AreEqual(1, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("fast", parsedArguments.NamedArguments["--mode"]);
    }

    // A key with the separator present but nothing after it gives an empty value.
    [TestMethod]
    public void Parse_KeyWithSeparatorAndNoValue_GivesAnEmptyValue()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse("--mode=");

        Assert.AreEqual(1, parsedArguments.NamedArguments.Count);
        Assert.AreEqual(string.Empty, parsedArguments.NamedArguments["--mode"]);
    }

    #endregion

    private static void AssertNothingWasParsed(ParsedArguments parsedArguments)
    {
        Assert.AreEqual(0, parsedArguments.Arguments.Count);
        Assert.AreEqual(0, parsedArguments.IntegerArguments.Count);
        Assert.AreEqual(0, parsedArguments.DecimalArguments.Count);
        Assert.AreEqual(0, parsedArguments.StringArguments.Count);
        Assert.AreEqual(0, parsedArguments.NamedArguments.Count);
    }
}
