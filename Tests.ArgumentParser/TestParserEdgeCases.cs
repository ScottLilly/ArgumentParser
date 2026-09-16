using System.Globalization;

using ArgumentParser;

namespace Tests.ArgumentParser;

/// <summary>
/// The edge cases listed in issue #51. A few still pin behavior that is known to be wrong,
/// each one named in a comment with the issue that will change it. They are here so that
/// fixing one of those issues cannot pass unnoticed: the test fails and gets rewritten to
/// assert the corrected behavior, which is what happened to the tests for issues #40, #41,
/// #43 and #44.
/// </summary>
[TestClass]
public class TestParserEdgeCases
{
    #region Missing and empty input

    // Issue #42: this should be a guarded ArgumentNullException, or an empty result.
    [TestMethod]
    public void Parse_NullString_ThrowsNullReferenceException()
    {
        Parser parser = new Parser();

        Assert.ThrowsExactly<NullReferenceException>(() => parser.Parse((string)null!));
    }

    // Issue #42: the string[] overload fails in string.Join rather than in Parse itself.
    [TestMethod]
    public void Parse_NullStringArray_ThrowsArgumentNullException()
    {
        Parser parser = new Parser();

        Assert.ThrowsExactly<ArgumentNullException>(() => parser.Parse((string[])null!));
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

    // Issue #39: the earlier value is lost, with nothing reported to the caller.
    [TestMethod]
    public void Parse_KeyGivenMoreThanOnce_KeepsOnlyTheLastValue()
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse("--mode:a --mode:b");

        Assert.AreEqual(2, parsedArguments.Arguments.Count);
        Assert.AreEqual(1, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("b", parsedArguments.NamedArguments["--mode"]);
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
            .Parse("mode=test,other:value");

        Assert.AreEqual(2, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("test", parsedArguments.NamedArguments["mode"]);
        Assert.AreEqual("value", parsedArguments.NamedArguments["other"]);
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
