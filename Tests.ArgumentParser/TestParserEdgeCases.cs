using System.Globalization;

using ArgumentParser;

namespace Tests.ArgumentParser;

/// <summary>
/// The edge cases listed in issue #51. Several of these pin behavior that is known to be
/// wrong, each one named in a comment with the issue that will change it. They are here so
/// that fixing one of those issues cannot pass unnoticed: the test fails and gets rewritten
/// to assert the corrected behavior.
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

    // Issue #40: matching is separator-major, so the parser looks for every ':' anywhere in
    // the argument before it considers the '=' that actually separates key from value.
    [TestMethod]
    [DataRow(@"--out=C:\build", "--out=C", @"\build", DisplayName = "Drive letter colon in a Windows path")]
    [DataRow("--url=http://x", "--url=http", "//x", DisplayName = "Scheme colon in a URL")]
    public void Parse_ValueContainingAnotherSeparator_SplitsAtTheWrongSeparator(
        string argument, string expectedKey, string expectedValue)
    {
        Parser parser = new Parser();

        ParsedArguments parsedArguments = parser.Parse(argument);

        Assert.AreEqual(1, parsedArguments.NamedArguments.Count);
        Assert.AreEqual(expectedValue, parsedArguments.NamedArguments[expectedKey]);
    }

    // The same argument splits correctly once '=' is the only key/value separator, which is
    // what identifies the cause as separator ordering rather than the value's content.
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

    // Issue #41: int.TryParse and decimal.TryParse use the current culture, so which bucket
    // an argument lands in depends on the machine the program runs on. CurrentCulture is set
    // here rather than relying on the test machine's own, so this fails everywhere or nowhere.
    [TestMethod]
    public void Parse_CommaDecimalUnderAGermanCulture_IsClassifiedAsADecimal()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            ParsedArguments parsedArguments = new Parser().Parse("123,45");

            // Under the invariant culture this is not a number at all, and would be a string.
            Assert.AreEqual(1, parsedArguments.DecimalArguments.Count);
            Assert.AreEqual(123.45m, parsedArguments.DecimalArguments[0]);
            Assert.AreEqual(0, parsedArguments.StringArguments.Count);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    // Issue #41: the same input, read as a value a thousand times larger.
    [TestMethod]
    public void Parse_PointDecimalUnderAGermanCulture_ReadsThePointAsAGroupSeparator()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            ParsedArguments parsedArguments = new Parser().Parse("123.45");

            Assert.AreEqual(1, parsedArguments.DecimalArguments.Count);
            Assert.AreEqual(12345m, parsedArguments.DecimalArguments[0]);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    #endregion

    #region Fluent parser with nothing configured

    // Issue #43: Create().Parse(...) hands the Parser two empty arrays rather than letting it
    // fall back to its defaults, so no key/value separator is ever matched.
    [TestMethod]
    public void FluentParse_NoSeparatorsConfigured_FindsNoNamedArguments()
    {
        ParsedArguments parsedArguments =
            FluentArgumentParser.Create().Parse("--mode:test extra 1");

        Assert.AreEqual(3, parsedArguments.Arguments.Count);
        Assert.AreEqual(0, parsedArguments.NamedArguments.Count);
        Assert.AreEqual(2, parsedArguments.StringArguments.Count);
        Assert.AreEqual(1, parsedArguments.IntegerArguments.Count);
        CollectionAssert.Contains(parsedArguments.StringArguments.ToList(), "--mode:test");
    }

    // The non-fluent Parser does fall back to its defaults for the same input, which is the
    // inconsistency issue #43 describes.
    [TestMethod]
    public void Parse_NoSeparatorsPassedToTheConstructor_UsesTheDefaultSeparators()
    {
        ParsedArguments parsedArguments = new Parser().Parse("--mode:test extra 1");

        Assert.AreEqual(1, parsedArguments.NamedArguments.Count);
        Assert.AreEqual("test", parsedArguments.NamedArguments["--mode"]);
    }

    #endregion

    #region EnumArgumentsOfType with a non-enum type

    // Issue #44: the constraint is "struct", so a non-enum struct compiles and then fails at
    // run time inside Enum.TryParse. An "enum" constraint would make this a compile error.
    [TestMethod]
    public void EnumArgumentsOfType_NonEnumStruct_ThrowsWhenEnumerated()
    {
        ParsedArguments parsedArguments = new Parser().Parse("production 1");

        Assert.ThrowsExactly<ArgumentException>(
            () => parsedArguments.EnumArgumentsOfType<int>().ToList());
    }

    // The failure is deferred, because the query is lazy. Nothing throws until it is walked.
    [TestMethod]
    public void EnumArgumentsOfType_NonEnumStruct_DoesNotThrowUntilEnumerated()
    {
        ParsedArguments parsedArguments = new Parser().Parse("production 1");

        IEnumerable<int> unenumerated = parsedArguments.EnumArgumentsOfType<int>();

        Assert.IsNotNull(unenumerated);
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
