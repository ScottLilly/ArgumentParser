using ArgumentParser;

namespace Tests.ArgumentParser;

/// <summary>
/// Issues #46 and #47. A declared schema, and the error reporting that only becomes possible
/// once the options are declared.
/// </summary>
[TestClass]
public class TestArgumentSchema
{
    private static ArgumentSchema BuildStandardSchema() =>
        ArgumentSchema.Create()
            .Option<string>("--output", alias: "-o", required: true, repeatable: true,
                description: "Where to write the report")
            .Option<int>("--timeout", defaultValue: 60,
                description: "Seconds before the run is abandoned")
            .Flag("--verbose", alias: "-v")
            .Build();

    #region Reading values

    [TestMethod]
    public void Parse_InlineValue_IsConvertedToTheDeclaredType()
    {
        SchemaParseResult result = BuildStandardSchema().Parse("--output=a.json --timeout=30");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(30, result.ValueOf<int>("--timeout"));
        Assert.AreEqual("a.json", result.ValueOf<string>("--output"));
    }

    // The main thing a declaration buys: the parser knows --timeout takes a value, so the
    // value can be the next argument rather than needing a separator.
    [TestMethod]
    [DataRow("--output a.json --timeout 30", DisplayName = "Space separated")]
    [DataRow("--output=a.json --timeout=30", DisplayName = "Equals separated")]
    [DataRow("--output:a.json --timeout:30", DisplayName = "Colon separated")]
    public void Parse_ValueWrittenEitherWayRound_GivesTheSameResult(string arguments)
    {
        SchemaParseResult result = BuildStandardSchema().Parse(arguments);

        Assert.IsTrue(result.Success, result.ErrorText());
        Assert.AreEqual("a.json", result.ValueOf<string>("--output"));
        Assert.AreEqual(30, result.ValueOf<int>("--timeout"));
    }

    [TestMethod]
    public void Parse_OptionNotGiven_FallsBackToTheDeclaredDefault()
    {
        SchemaParseResult result = BuildStandardSchema().Parse("--output=a.json");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(60, result.ValueOf<int>("--timeout"));
        Assert.IsFalse(result.IsSet("--timeout"));
    }

    [TestMethod]
    public void Parse_Alias_IsTheSameOptionAsItsFullName()
    {
        SchemaParseResult result = BuildStandardSchema().Parse("-o a.json -v");

        Assert.IsTrue(result.Success, result.ErrorText());
        Assert.AreEqual("a.json", result.ValueOf<string>("--output"));
        Assert.AreEqual("a.json", result.ValueOf<string>("-o"));
        Assert.IsTrue(result.ValueOf<bool>("--verbose"));
    }

    [TestMethod]
    public void Parse_Flag_IsTrueByItsPresenceAlone()
    {
        SchemaParseResult result = BuildStandardSchema().Parse("--output=a.json --verbose");

        Assert.IsTrue(result.Success, result.ErrorText());
        Assert.IsTrue(result.IsSet("--verbose"));
        Assert.IsTrue(result.ValueOf<bool>("--verbose"));
    }

    [TestMethod]
    public void Parse_FlagNotGiven_IsFalse()
    {
        SchemaParseResult result = BuildStandardSchema().Parse("--output=a.json");

        Assert.IsFalse(result.IsSet("--verbose"));
        Assert.IsFalse(result.ValueOf<bool>("--verbose"));
    }

    // A flag can still be written out in full, which a script generating a command line
    // often finds easier than conditionally including the argument at all.
    [TestMethod]
    public void Parse_FlagWrittenOutInFull_UsesTheValueGiven()
    {
        SchemaParseResult result = BuildStandardSchema().Parse("--output=a.json --verbose=false");

        Assert.IsTrue(result.Success, result.ErrorText());
        Assert.IsTrue(result.IsSet("--verbose"));
        Assert.IsFalse(result.ValueOf<bool>("--verbose"));
    }

    [TestMethod]
    public void Parse_RepeatableOptionGivenSeveralTimes_KeepsEveryValue()
    {
        SchemaParseResult result =
            BuildStandardSchema().Parse("--output a.json --output b.sarif --output c.xml");

        Assert.IsTrue(result.Success, result.ErrorText());
        CollectionAssert.AreEqual(
            new[] { "a.json", "b.sarif", "c.xml" },
            result.AllValuesOf<string>("--output").ToArray());
        Assert.AreEqual("c.xml", result.ValueOf<string>("--output"));
    }

    #endregion

    #region Positional arguments

    [TestMethod]
    public void Parse_ArgumentsThatAreNotOptions_AreHandedBackAsPositional()
    {
        SchemaParseResult result =
            BuildStandardSchema().Parse("input.txt --output=a.json other.txt");

        Assert.IsTrue(result.Success, result.ErrorText());
        CollectionAssert.AreEqual(
            new[] { "input.txt", "other.txt" }, result.PositionalArguments.ToArray());
    }

    // A negative number is not an unknown option, however much it looks like one.
    [TestMethod]
    [DataRow("-5", DisplayName = "Negative integer")]
    [DataRow("-4.5", DisplayName = "Negative decimal")]
    public void Parse_NegativeNumber_IsPositionalRatherThanAnUnknownOption(string argument)
    {
        SchemaParseResult result = BuildStandardSchema().Parse($"--output=a.json {argument}");

        Assert.IsTrue(result.Success, result.ErrorText());
        CollectionAssert.AreEqual(new[] { argument }, result.PositionalArguments.ToArray());
    }

    #endregion

    #region Errors

    [TestMethod]
    public void Parse_MisspelledOption_IsReportedRatherThanIgnored()
    {
        SchemaParseResult result = BuildStandardSchema().Parse("--output=a.json --verbse");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(1, result.Errors.Count);
        Assert.AreEqual(ParseErrorKind.UnknownOption, result.Errors[0].Kind);
        Assert.AreEqual("--verbse", result.Errors[0].OptionName);
    }

    [TestMethod]
    public void Parse_MisspelledOptionWithAValue_ReportsTheNameRatherThanTheWholeArgument()
    {
        SchemaParseResult result = BuildStandardSchema().Parse("--output=a.json --timout=30");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ParseErrorKind.UnknownOption, result.Errors[0].Kind);
        Assert.AreEqual("--timout", result.Errors[0].OptionName);
    }

    [TestMethod]
    public void Parse_RequiredOptionMissing_IsReported()
    {
        SchemaParseResult result = BuildStandardSchema().Parse("--timeout=30");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(1, result.Errors.Count);
        Assert.AreEqual(ParseErrorKind.MissingRequiredOption, result.Errors[0].Kind);
        Assert.AreEqual("--output", result.Errors[0].OptionName);
    }

    [TestMethod]
    public void Parse_OptionWithNoValueAtTheEnd_IsReported()
    {
        SchemaParseResult result = BuildStandardSchema().Parse("--output");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ParseErrorKind.MissingValue, result.Errors[0].Kind);
        Assert.AreEqual("--output", result.Errors[0].OptionName);
    }

    // "--output --verbose" is a value left out, not a request to write to a file called
    // "--verbose".
    [TestMethod]
    public void Parse_OptionFollowedByAnotherOption_IsAMissingValueRatherThanSwallowingIt()
    {
        SchemaParseResult result = BuildStandardSchema().Parse("--output --verbose");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ParseErrorKind.MissingValue, result.Errors[0].Kind);
        Assert.IsTrue(result.IsSet("--verbose"));
    }

    [TestMethod]
    public void Parse_ValueOfTheWrongType_IsReported()
    {
        SchemaParseResult result = BuildStandardSchema().Parse("--output=a.json --timeout=abc");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ParseErrorKind.UnconvertibleValue, result.Errors[0].Kind);
        Assert.AreEqual("--timeout", result.Errors[0].OptionName);
        Assert.AreEqual("abc", result.Errors[0].Value);
    }

    [TestMethod]
    public void Parse_OptionRepeatedThatWasNotDeclaredRepeatable_IsReported()
    {
        SchemaParseResult result =
            BuildStandardSchema().Parse("--output=a.json --timeout=30 --timeout=60");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ParseErrorKind.OptionNotRepeatable, result.Errors[0].Kind);
        Assert.AreEqual("--timeout", result.Errors[0].OptionName);
    }

    // A command line application usually wants to print every problem at once rather than
    // making the user fix them one run at a time, which is why this is a result and not an
    // exception.
    [TestMethod]
    public void Parse_SeveralProblemsAtOnce_ReportsAllOfThem()
    {
        SchemaParseResult result = BuildStandardSchema().Parse("--timeout=abc --verbse");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(3, result.Errors.Count);
        CollectionAssert.AreEquivalent(
            new[]
            {
                ParseErrorKind.UnknownOption,
                ParseErrorKind.MissingRequiredOption,
                ParseErrorKind.UnconvertibleValue
            },
            result.Errors.Select(e => e.Kind).ToArray());
    }

    [TestMethod]
    public void ErrorText_SeveralErrors_IsOneMessagePerLine()
    {
        SchemaParseResult result = BuildStandardSchema().Parse("--timeout=abc --verbse");

        Assert.AreEqual(3, result.ErrorText().Split('\n').Length);
    }

    [TestMethod]
    public void Parse_NothingWrong_HasNoErrors()
    {
        SchemaParseResult result = BuildStandardSchema().Parse("--output=a.json");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, result.Errors.Count);
        Assert.AreEqual(string.Empty, result.ErrorText());
    }

    #endregion

    #region Enums, which issue #2 asked for

    private static ArgumentSchema BuildEnumSchema() =>
        ArgumentSchema.Create()
            .Option<EmployeeType>("--type", defaultValue: EmployeeType.Production)
            .Build();

    [TestMethod]
    [DataRow("Sales", EmployeeType.Sales, DisplayName = "Exact casing")]
    [DataRow("marketing", EmployeeType.Marketing, DisplayName = "Lower case")]
    [DataRow("PRODUCTION", EmployeeType.Production, DisplayName = "Upper case")]
    public void Parse_EnumOption_ConvertsTheNameWhateverItsCasing(
        string argument, EmployeeType expected)
    {
        SchemaParseResult result = BuildEnumSchema().Parse($"--type={argument}");

        Assert.IsTrue(result.Success, result.ErrorText());
        Assert.AreEqual(expected, result.ValueOf<EmployeeType>("--type"));
    }

    // The numeric form is deliberately rejected, since it would let any number at all match
    // any enum. Issue #44 noted this when the same trap existed on EnumArgumentsOfType.
    [TestMethod]
    [DataRow("1", DisplayName = "A defined ordinal")]
    [DataRow("99", DisplayName = "An undefined ordinal")]
    [DataRow("Sales,Marketing", DisplayName = "A combination")]
    [DataRow("nonsense", DisplayName = "Not a member")]
    public void Parse_EnumOptionGivenSomethingOtherThanAName_IsReported(string argument)
    {
        SchemaParseResult result = BuildEnumSchema().Parse($"--type={argument}");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ParseErrorKind.UnconvertibleValue, result.Errors[0].Kind);
    }

    [TestMethod]
    public void Parse_EnumOptionNotGiven_FallsBackToTheDeclaredDefault()
    {
        SchemaParseResult result = BuildEnumSchema().Parse(string.Empty);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(EmployeeType.Production, result.ValueOf<EmployeeType>("--type"));
    }

    #endregion

    #region Name matching

    [TestMethod]
    public void Parse_NamesDifferingInCase_MatchByDefault()
    {
        SchemaParseResult result = BuildStandardSchema().Parse("--OUTPUT=a.json");

        Assert.IsTrue(result.Success, result.ErrorText());
        Assert.AreEqual("a.json", result.ValueOf<string>("--output"));
    }

    [TestMethod]
    public void Parse_OrdinalComparer_TreatsADifferentCasingAsAnUnknownOption()
    {
        ArgumentSchema schema =
            ArgumentSchema.Create()
                .WithComparer(StringComparer.Ordinal)
                .Option<string>("--output")
                .Build();

        SchemaParseResult result = schema.Parse("--OUTPUT=a.json");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ParseErrorKind.UnknownOption, result.Errors[0].Kind);
    }

    #endregion

    #region Quoting and the string[] overload

    [TestMethod]
    public void Parse_QuotedValueContainingASpace_StaysOneValue()
    {
        SchemaParseResult result =
            BuildStandardSchema().Parse(@"--output=""C:\Test\My Project.sln""");

        Assert.IsTrue(result.Success, result.ErrorText());
        Assert.AreEqual(@"C:\Test\My Project.sln", result.ValueOf<string>("--output"));
    }

    [TestMethod]
    public void Parse_StringArrayFromMain_KeepsAValueTheShellAlreadyUnquoted()
    {
        SchemaParseResult result =
            BuildStandardSchema().Parse(new[] { "--output", @"C:\Test\My Project.sln" });

        Assert.IsTrue(result.Success, result.ErrorText());
        Assert.AreEqual(@"C:\Test\My Project.sln", result.ValueOf<string>("--output"));
    }

    [TestMethod]
    public void Parse_NullInput_ReportsOnlyTheMissingRequiredOption()
    {
        SchemaParseResult result = BuildStandardSchema().Parse((string)null!);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(1, result.Errors.Count);
        Assert.AreEqual(ParseErrorKind.MissingRequiredOption, result.Errors[0].Kind);
    }

    #endregion

    #region ParseOrThrow

    [TestMethod]
    public void ParseOrThrow_ValidArguments_ReturnsTheResult()
    {
        SchemaParseResult result = BuildStandardSchema().ParseOrThrow("--output=a.json");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("a.json", result.ValueOf<string>("--output"));
    }

    [TestMethod]
    public void ParseOrThrow_InvalidArguments_ThrowsCarryingEveryError()
    {
        ArgumentParseException exception = Assert.ThrowsExactly<ArgumentParseException>(
            () => BuildStandardSchema().ParseOrThrow("--timeout=abc --verbse"));

        Assert.AreEqual(3, exception.Errors.Count);
        Assert.IsTrue(exception.Message.Contains("--verbse"));
    }

    [TestMethod]
    public void ParseOrThrow_StringArrayOverload_ThrowsTheSameWay()
    {
        Assert.ThrowsExactly<ArgumentParseException>(
            () => BuildStandardSchema().ParseOrThrow(new[] { "--verbse" }));
    }

    #endregion

    #region Misusing the API

    [TestMethod]
    public void ValueOf_NameThatWasNeverDeclared_Throws()
    {
        SchemaParseResult result = BuildStandardSchema().Parse("--output=a.json");

        Assert.ThrowsExactly<ArgumentException>(() => result.ValueOf<string>("--nope"));
    }

    [TestMethod]
    public void ValueOf_TypeThatDoesNotMatchTheDeclaration_Throws()
    {
        SchemaParseResult result = BuildStandardSchema().Parse("--output=a.json");

        Assert.ThrowsExactly<InvalidOperationException>(
            () => result.ValueOf<int>("--output"));
    }

    [TestMethod]
    public void Option_NameAlreadyDeclared_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            ArgumentSchema.Create()
                .Option<string>("--output")
                .Option<int>("--output")
                .Build());
    }

    [TestMethod]
    public void Option_NameClashingWithAnExistingAlias_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            ArgumentSchema.Create()
                .Option<string>("--output", alias: "-o")
                .Flag("-o")
                .Build());
    }

    [TestMethod]
    public void Option_UnsupportedType_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            ArgumentSchema.Create().Option<DateTime>("--when").Build());
    }

    #endregion
}
