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

    // Issue #60: everything after a bare "--" is positional, which is the only way to pass
    // a positional argument that starts with an option prefix.
    [TestMethod]
    public void Parse_EndOfOptionsMarker_MakesEverythingAfterItPositional()
    {
        SchemaParseResult result =
            BuildStandardSchema().Parse("--output=a.json -- --verbose --notanoption x");

        Assert.IsTrue(result.Success, result.ErrorText());
        Assert.IsFalse(result.IsSet("--verbose"));
        CollectionAssert.AreEqual(
            new[] { "--verbose", "--notanoption", "x" },
            result.PositionalArguments.ToArray());
    }

    // The marker itself is consumed. A second one is an ordinary positional argument,
    // since the options have already ended.
    [TestMethod]
    public void Parse_EndOfOptionsMarker_IsNotItselfPositional()
    {
        SchemaParseResult result = BuildStandardSchema().Parse("--output=a.json -- -- x");

        Assert.IsTrue(result.Success, result.ErrorText());
        CollectionAssert.AreEqual(
            new[] { "--", "x" }, result.PositionalArguments.ToArray());
    }

    [TestMethod]
    public void Parse_EndOfOptionsMarkerInAnArray_MakesEverythingAfterItPositional()
    {
        SchemaParseResult result =
            BuildStandardSchema().Parse(new[] { "--output=a.json", "--", "--verbose" });

        Assert.IsTrue(result.Success, result.ErrorText());
        Assert.IsFalse(result.IsSet("--verbose"));
        CollectionAssert.AreEqual(
            new[] { "--verbose" }, result.PositionalArguments.ToArray());
    }

    // Tied to the configured prefixes, so a schema whose users would never type "--" does
    // not quietly give it a meaning.
    [TestMethod]
    public void Parse_EndOfOptionsMarker_IsNotSpecialWhenTheDashPrefixesAreNotConfigured()
    {
        ArgumentSchema schema =
            ArgumentSchema.Create()
                .WithOptionPrefixes("/")
                .Flag("/verbose")
                .Build();

        SchemaParseResult result = schema.Parse("-- /verbose");

        Assert.IsTrue(result.Success, result.ErrorText());
        Assert.IsTrue(result.ValueOf<bool>("/verbose"));
        CollectionAssert.AreEqual(new[] { "--" }, result.PositionalArguments.ToArray());
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

    // Issue #59: a flag is exempt. "-v -v" is what someone types when they want more of
    // whatever the flag asks for, and no command line tool treats it as a mistake.
    [TestMethod]
    [DataRow("--verbose --verbose", DisplayName = "Repeated by name")]
    [DataRow("-v -v", DisplayName = "Repeated by alias")]
    [DataRow("--verbose -v", DisplayName = "Once by each")]
    public void Parse_FlagGivenMoreThanOnce_IsNotAnError(string arguments)
    {
        SchemaParseResult result =
            BuildStandardSchema().Parse($"--output=a.json {arguments}");

        Assert.IsTrue(result.Success, result.ErrorText());
        Assert.IsTrue(result.ValueOf<bool>("--verbose"));
    }

    // Every occurrence is still recorded, so a caller who wants "-vv means more verbose"
    // can count them.
    [TestMethod]
    public void Parse_FlagGivenMoreThanOnce_KeepsEveryOccurrence()
    {
        SchemaParseResult result =
            BuildStandardSchema().Parse("--output=a.json -v -v -v");

        Assert.AreEqual(3, result.AllValuesOf<bool>("--verbose").Count);
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

    // Issue #62. Enum values were matched with OrdinalIgnoreCase whatever the schema's
    // comparer was, so a schema that was strict about the case of its option names was not
    // strict about the case of its values.
    [TestMethod]
    public void Parse_EnumOptionOnAnOrdinalSchema_MatchesOnlyTheDeclaredCasing()
    {
        ArgumentSchema schema =
            ArgumentSchema.Create()
                .WithComparer(StringComparer.Ordinal)
                .Option<EmployeeType>("--type")
                .Build();

        Assert.IsTrue(schema.Parse("--type=Sales").Success);

        SchemaParseResult result = schema.Parse("--type=sales");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(ParseErrorKind.UnconvertibleValue, result.Errors[0].Kind);
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
        SchemaParseResult result = BuildStandardSchema().Parse((string?)null);

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

    // Issue #61. A required option is an error when it is left out, which is the only time
    // a default could apply, so the two together describe something that cannot happen.
    [TestMethod]
    public void Option_RequiredWithADefault_Throws()
    {
        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(() =>
            ArgumentSchema.Create()
                .Option<string>("--output", required: true, defaultValue: "a.json"));

        Assert.IsTrue(exception.Message.Contains("could never be used"), exception.Message);
    }

    [TestMethod]
    public void Option_RequiredWithADefaultOfTheTypesOwnZero_AlsoThrows()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            ArgumentSchema.Create().Option<int>("--timeout", required: true, defaultValue: 0));
    }

    [TestMethod]
    public void Option_RequiredWithNoDefault_IsFine()
    {
        ArgumentSchema schema =
            ArgumentSchema.Create().Option<string>("--output", required: true).Build();

        Assert.IsTrue(schema.Options[0].IsRequired);
        Assert.IsNull(schema.Options[0].DefaultValue);
    }

    // Issue #61. An unprefixed name is never option-shaped, so "output" alone would be read
    // as a positional argument while "output=a.json" matched the option.
    [TestMethod]
    public void Build_OptionNameWithNoPrefix_Throws()
    {
        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(() =>
            ArgumentSchema.Create().Option<string>("output").Build());

        Assert.IsTrue(exception.Message.Contains("'output'"), exception.Message);
    }

    [TestMethod]
    public void Build_AliasWithNoPrefix_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            ArgumentSchema.Create().Option<string>("--output", alias: "o").Build());
    }

    [TestMethod]
    public void Build_FlagNameWithNoPrefix_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            ArgumentSchema.Create().Flag("verbose").Build());
    }

    // The check runs at Build rather than at the declaration, because the prefixes can be
    // configured after the options are declared.
    [TestMethod]
    public void Build_NameMatchingAPrefixConfiguredAfterwards_IsFine()
    {
        ArgumentSchema schema =
            ArgumentSchema.Create()
                .Option<string>("/output")
                .WithOptionPrefixes("/")
                .Build();

        Assert.IsTrue(schema.Parse("/output=a.json").Success);
    }

    [TestMethod]
    public void Build_NameMatchingTheDefaultPrefixesAfterTheyAreReplaced_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            ArgumentSchema.Create()
                .Option<string>("--output")
                .WithOptionPrefixes("/")
                .Build());
    }

    // The automatic help option is the library's declaration, not the caller's, and it is
    // matched by name before anything asks what shape it is.
    [TestMethod]
    public void Build_CustomPrefixes_DoesNotRejectTheAutomaticHelpOption()
    {
        ArgumentSchema schema =
            ArgumentSchema.Create()
                .WithOptionPrefixes("/")
                .Flag("/verbose")
                .Build();

        Assert.IsTrue(schema.Parse("--help").HelpRequested);
    }

    [TestMethod]
    public void Option_UnsupportedType_Throws()
    {
        NotSupportedException exception = Assert.ThrowsExactly<NotSupportedException>(() =>
            ArgumentSchema.Create().Option<DateTime>("--when").Build());

        Assert.IsTrue(exception.Message.StartsWith("DateTime is not a supported option type"));
        Assert.IsFalse(exception.Message.Contains("Parameter"));
    }

    // Issue #53. An unconstrained T? default collapsed to default(T) for a value type, so
    // every int option claimed a default of 0 it was never given.
    [TestMethod]
    public void Option_NoDefaultDeclared_HasNoDefaultValue()
    {
        ArgumentSchema schema =
            ArgumentSchema.Create()
                .Option<int>("--count")
                .Option<bool>("--dry")
                .Option<EmployeeType>("--type")
                .Option<int>("--declared", defaultValue: 0)
                .Build();

        Assert.IsNull(schema.Options[0].DefaultValue);
        Assert.IsNull(schema.Options[1].DefaultValue);
        Assert.IsNull(schema.Options[2].DefaultValue);
        Assert.AreEqual(0, schema.Options[3].DefaultValue);
    }

    #endregion

    #region Empty and quoted values

    // Issue #55. A quoted empty string was dropped by the tokenizer, so "--name """ read
    // as an option with its value left out.
    [TestMethod]
    [DataRow("--name \"\"", DisplayName = "Separate quoted value")]
    [DataRow("--name=\"\"", DisplayName = "Inline quoted value")]
    public void Parse_QuotedEmptyValue_IsAnEmptyString(string arguments)
    {
        ArgumentSchema schema =
            ArgumentSchema.Create().Option<string>("--name").Build();

        SchemaParseResult result = schema.Parse(arguments);

        Assert.IsTrue(result.Success, result.ErrorText());
        Assert.AreEqual(string.Empty, result.ValueOf<string>("--name"));
    }

    // Issue #63. The schema trims the same way the untyped Parser does, and for the same
    // reason: the whitespace someone meant to keep is the whitespace in the middle.
    [TestMethod]
    public void Parse_QuotedValueWithSpacesAtBothEnds_IsTrimmedButKeepsItsInnerSpaces()
    {
        ArgumentSchema schema =
            ArgumentSchema.Create().Option<string>("--name").Build();

        SchemaParseResult result = schema.Parse("--name \"  a   b  \"");

        Assert.IsTrue(result.Success, result.ErrorText());
        Assert.AreEqual("a   b", result.ValueOf<string>("--name"));
    }

    [TestMethod]
    public void Parse_EmptyStringArrayElement_IsAnEmptyValue()
    {
        ArgumentSchema schema =
            ArgumentSchema.Create().Option<string>("--name").Build();

        SchemaParseResult result = schema.Parse(new[] { "--name", "" });

        Assert.IsTrue(result.Success, result.ErrorText());
        Assert.AreEqual(string.Empty, result.ValueOf<string>("--name"));
    }

    // Issue #57. "--timeout=" was reported as an unconvertible '' rather than as a value
    // that was left out.
    [TestMethod]
    [DataRow("--timeout=", DisplayName = "On a command line")]
    [DataRow("-t=", DisplayName = "By alias")]
    public void Parse_InlineSeparatorWithNothingAfterIt_IsAMissingValue(string arguments)
    {
        ArgumentSchema schema =
            ArgumentSchema.Create().Option<int>("--timeout", alias: "-t").Build();

        SchemaParseResult result = schema.Parse(arguments);

        Assert.AreEqual(1, result.Errors.Count);
        Assert.AreEqual(ParseErrorKind.MissingValue, result.Errors[0].Kind);
        Assert.AreEqual("--timeout", result.Errors[0].OptionName);
    }

    [TestMethod]
    public void Parse_StringArrayElementWithInlineSeparatorAndNoValue_IsAMissingValue()
    {
        ArgumentSchema schema =
            ArgumentSchema.Create().Option<int>("--timeout").Build();

        SchemaParseResult result = schema.Parse(new[] { "--timeout=" });

        Assert.AreEqual(ParseErrorKind.MissingValue, result.Errors[0].Kind);
    }

    // Issue #56. The array used to be joined with spaces and split again, and an element
    // containing a quote was not requoted, so it was cut into pieces.
    [TestMethod]
    public void Parse_StringArrayElementContainingAQuote_SurvivesAsGiven()
    {
        ArgumentSchema schema =
            ArgumentSchema.Create().Option<string>("--name").Build();

        SchemaParseResult result = schema.Parse(new[] { "--name", "say \"hi\" now" });

        Assert.IsTrue(result.Success, result.ErrorText());
        Assert.AreEqual("say \"hi\" now", result.ValueOf<string>("--name"));
        Assert.AreEqual(0, result.PositionalArguments.Count);
    }

    [TestMethod]
    public void Parse_StringArrayElementContainingASpace_IsOneToken()
    {
        ArgumentSchema schema =
            ArgumentSchema.Create().Option<string>("--output").Build();

        SchemaParseResult result =
            schema.Parse(new[] { "--output", @"C:\My Project\out.json", "a b" });

        Assert.AreEqual(@"C:\My Project\out.json", result.ValueOf<string>("--output"));
        CollectionAssert.AreEqual(new[] { "a b" }, result.PositionalArguments.ToArray());
    }

    #endregion
}
