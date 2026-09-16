using ArgumentParser;

namespace Tests.ArgumentParser;

/// <summary>
/// Issue #48. Help text built from the same declarations that parse the arguments, so the two
/// cannot drift apart. Nothing is written to the console: HelpText returns a string and the
/// caller decides where it goes.
/// </summary>
[TestClass]
public class TestArgumentSchemaHelpText
{
    private static ArgumentSchema BuildDocumentedSchema() =>
        ArgumentSchema.Create()
            .WithApplicationName("myapp")
            .WithUsage("myapp <input> [options]")
            .WithDescription("Checks a solution and writes a report.")
            .Option<string>("--output", alias: "-o", required: true, repeatable: true,
                description: "Where to write the report", valueName: "path")
            .Option<int>("--timeout", defaultValue: 60,
                description: "Seconds before the run is abandoned")
            .Flag("--verbose", alias: "-v", description: "Print each step as it runs")
            .Build();

    #region Layout

    [TestMethod]
    public void HelpText_DocumentedSchema_LaysOutTheWholeThing()
    {
        string expected = string.Join(Environment.NewLine,
            "Checks a solution and writes a report.",
            "",
            "Usage: myapp <input> [options]",
            "",
            "  -o, --output <path>  Where to write the report (required, repeatable)",
            "      --timeout <int>  Seconds before the run is abandoned (default: 60)",
            "  -v, --verbose        Print each step as it runs",
            "  -h, --help           Show this help");

        Assert.AreEqual(expected, BuildDocumentedSchema().HelpText());
    }

    [TestMethod]
    public void HelpText_NoApplicationNameOrDescription_IsJustTheOptions()
    {
        string helpText = ArgumentSchema.Create().Option<int>("--count").Build().HelpText();

        Assert.IsFalse(helpText.Contains("Usage:"));
        Assert.IsTrue(helpText.StartsWith("      --count <int>"));
    }

    // An application name with no usage line of its own still gets one.
    [TestMethod]
    public void HelpText_ApplicationNameWithNoUsageLine_BuildsTheUsageLineFromIt()
    {
        ArgumentSchema schema =
            ArgumentSchema.Create().WithApplicationName("myapp").Flag("--verbose").Build();

        Assert.IsTrue(schema.HelpText().Contains("Usage: myapp [options]"));
        Assert.AreEqual("myapp [options]", schema.Usage);
    }

    [TestMethod]
    public void HelpText_UsageLineGivenExplicitly_OverridesTheBuiltOne()
    {
        ArgumentSchema schema =
            ArgumentSchema.Create()
                .WithApplicationName("myapp")
                .WithUsage("myapp <input> [options]")
                .Flag("--verbose")
                .Build();

        Assert.IsTrue(schema.HelpText().Contains("Usage: myapp <input> [options]"));
    }

    [TestMethod]
    public void HelpText_NoLine_HasTrailingWhitespace()
    {
        string[] lines = BuildDocumentedSchema().HelpText()
            .Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        foreach (string line in lines)
        {
            Assert.AreEqual(line.TrimEnd(), line, $"'{line}' has trailing whitespace.");
        }
    }

    #endregion

    #region Wrapping

    // Wrapped at the width asked for rather than the console's, so redirected output is
    // stable. Issue #48 called that out specifically.
    [TestMethod]
    [DataRow(80)]
    [DataRow(60)]
    [DataRow(120)]
    public void HelpText_LongDescription_WrapsAtTheWidthAskedFor(int width)
    {
        ArgumentSchema schema =
            ArgumentSchema.Create()
                .Option<string>("--output",
                    description: "This description is quite long as well, so it has to wrap onto another line to fit inside the width.")
                .Build();

        string[] lines = schema.HelpText(width)
            .Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        foreach (string line in lines)
        {
            Assert.IsTrue(line.Length <= width, $"'{line}' is {line.Length} characters.");
        }
    }

    [TestMethod]
    public void HelpText_LongDescription_IndentsTheContinuationToTheDescriptionColumn()
    {
        ArgumentSchema schema =
            ArgumentSchema.Create()
                .Option<string>("--output",
                    description: "This description is quite long as well, so it has to wrap onto another line.")
                .Build();

        string[] lines = schema.HelpText(60)
            .Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        int descriptionColumn = lines[0].IndexOf("This", StringComparison.Ordinal);

        Assert.IsTrue(descriptionColumn > 0);
        Assert.AreEqual(descriptionColumn, lines[1].Length - lines[1].TrimStart().Length);
    }

    // A name too long for the column takes a line to itself rather than pushing every other
    // description across the page to meet it.
    [TestMethod]
    public void HelpText_NameTooLongForTheColumn_PutsItsDescriptionOnTheNextLine()
    {
        ArgumentSchema schema =
            ArgumentSchema.Create()
                .Option<string>("--an-extremely-long-option-name-here", alias: "-x",
                    description: "Does something")
                .Option<int>("--n", description: "A short one")
                .Build();

        string[] lines = schema.HelpText()
            .Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        Assert.IsTrue(lines[0].TrimEnd().EndsWith("<value>"));
        Assert.IsFalse(lines[0].Contains("Does something"));
        Assert.IsTrue(lines[1].TrimStart().StartsWith("Does something"));
        Assert.IsTrue(lines[2].Contains("--n <int>"));
    }

    // Issue #64: a newline or a tab in a description is a word break like any other
    // whitespace. Left intact, one of them would wreck the column layout for every option.
    [TestMethod]
    [DataRow("line one\nline two", DisplayName = "Newline")]
    [DataRow("line one\r\nline two", DisplayName = "Carriage return and newline")]
    [DataRow("line one\tline two", DisplayName = "Tab")]
    public void HelpText_DescriptionContainingWhitespace_CollapsesItToASingleSpace(
        string description)
    {
        ArgumentSchema schema =
            ArgumentSchema.Create()
                .WithoutHelpOption()
                .Option<string>("--a", description: description)
                .Build();

        string helpText = schema.HelpText();

        Assert.AreEqual(1,
            helpText.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Length);
        Assert.IsTrue(helpText.EndsWith("line one line two", StringComparison.Ordinal),
            helpText);
    }

    #endregion

    #region What each line says

    [TestMethod]
    public void HelpText_RequiredAndRepeatableOption_SaysSo()
    {
        Assert.IsTrue(BuildDocumentedSchema().HelpText()
            .Contains("(required, repeatable)"));
    }

    [TestMethod]
    public void HelpText_OptionWithADefault_SaysWhatItIs()
    {
        Assert.IsTrue(BuildDocumentedSchema().HelpText().Contains("(default: 60)"));
    }

    // A required option has no useful default to report, so it is not mentioned.
    [TestMethod]
    public void HelpText_RequiredOption_DoesNotClaimADefault()
    {
        string line = BuildDocumentedSchema().HelpText()
            .Split(new[] { Environment.NewLine }, StringSplitOptions.None)
            .Single(l => l.Contains("--output"));

        Assert.IsFalse(line.Contains("default"));
    }

    [TestMethod]
    public void HelpText_Flag_ShowsNoValuePlaceholder()
    {
        string line = BuildDocumentedSchema().HelpText()
            .Split(new[] { Environment.NewLine }, StringSplitOptions.None)
            .Single(l => l.Contains("--verbose"));

        Assert.IsTrue(line.Contains("-v, --verbose "));
        Assert.IsFalse(line.Contains("<"));
    }

    [TestMethod]
    public void HelpText_ValueNameGiven_IsUsedInsteadOfTheTypeDerivedOne()
    {
        Assert.IsTrue(BuildDocumentedSchema().HelpText().Contains("--output <path>"));
    }

    [TestMethod]
    [DataRow(typeof(int), "<int>", DisplayName = "int")]
    [DataRow(typeof(decimal), "<number>", DisplayName = "decimal")]
    [DataRow(typeof(string), "<value>", DisplayName = "string")]
    public void HelpText_NoValueName_DerivesOneFromTheDeclaredType(
        Type declaredType, string expected)
    {
        IArgumentSchemaBuilder builder = ArgumentSchema.Create();

        if (declaredType == typeof(int)) builder.Option<int>("--x");
        else if (declaredType == typeof(decimal)) builder.Option<decimal>("--x");
        else builder.Option<string>("--x");

        Assert.IsTrue(builder.Build().HelpText().Contains("--x " + expected));
    }

    // Pipe separated, so the values do not read as more notes alongside "default:".
    [TestMethod]
    public void HelpText_EnumOption_ListsWhatItAccepts()
    {
        ArgumentSchema schema =
            ArgumentSchema.Create()
                .Option<EmployeeType>("--type", description: "Which team")
                .Build();

        // Wide enough that the note is on one line, since wrapping is covered above. No
        // default was declared, so none is claimed (issue #53).
        Assert.IsTrue(schema.HelpText(120)
            .Contains("Which team (one of: Production|Sales|Marketing)"));
    }

    #endregion

    #region The automatic help option

    [TestMethod]
    [DataRow("--help", DisplayName = "Full name")]
    [DataRow("-h", DisplayName = "Short form")]
    public void Parse_HelpAsked_IsReportedThroughTheResult(string argument)
    {
        SchemaParseResult result = BuildDocumentedSchema().Parse(argument);

        Assert.IsTrue(result.HelpRequested);
    }

    // Asking what the options are is not the moment to be told one is missing.
    [TestMethod]
    public void Parse_HelpAsked_SuppressesTheMissingRequiredOptionThatWouldOtherwiseReport()
    {
        SchemaParseResult withoutHelp = BuildDocumentedSchema().Parse(string.Empty);
        SchemaParseResult withHelp = BuildDocumentedSchema().Parse("--help");

        Assert.IsFalse(withoutHelp.Success);
        Assert.AreEqual(ParseErrorKind.MissingRequiredOption, withoutHelp.Errors[0].Kind);

        Assert.IsTrue(withHelp.Success);
        Assert.AreEqual(0, withHelp.Errors.Count);
    }

    [TestMethod]
    public void Parse_HelpAskedAlongsideAnUnknownOption_StillJustShowsHelp()
    {
        SchemaParseResult result = BuildDocumentedSchema().Parse("--help --nonsense");

        Assert.IsTrue(result.HelpRequested);
        Assert.IsTrue(result.Success);
    }

    [TestMethod]
    public void Parse_HelpNotAsked_LeavesHelpRequestedFalse()
    {
        SchemaParseResult result = BuildDocumentedSchema().Parse("--output=a.json");

        Assert.IsFalse(result.HelpRequested);
        Assert.IsTrue(result.Success);
    }

    [TestMethod]
    public void Build_ByDefault_AddsTheHelpOptionLast()
    {
        ArgumentSchema schema = BuildDocumentedSchema();

        Assert.AreEqual("--help", schema.Options[schema.Options.Count - 1].Name);
    }

    [TestMethod]
    public void Build_WithoutHelpOption_LeavesItOut()
    {
        ArgumentSchema schema =
            ArgumentSchema.Create().WithoutHelpOption().Flag("--verbose").Build();

        Assert.AreEqual(1, schema.Options.Count);
        Assert.IsFalse(schema.HelpText().Contains("--help"));

        SchemaParseResult result = schema.Parse("--help");

        Assert.IsFalse(result.HelpRequested);
        Assert.IsFalse(result.Success);
        Assert.AreEqual(ParseErrorKind.UnknownOption, result.Errors[0].Kind);
    }

    // Taking a name the caller has already used would be worse than having no automatic help,
    // so the declaration wins and nothing is added.
    [TestMethod]
    [DataRow("--help", DisplayName = "Caller declared the full name")]
    [DataRow("-h", DisplayName = "Caller declared the short form")]
    public void Build_CallerAlreadyUsedAHelpName_DoesNotAddTheAutomaticOne(string name)
    {
        ArgumentSchema schema =
            ArgumentSchema.Create().Option<string>(name, description: "Mine").Build();

        Assert.AreEqual(1, schema.Options.Count);
        Assert.AreEqual(name, schema.Options[0].Name);

        SchemaParseResult result = schema.Parse($"{name} something");

        Assert.IsFalse(result.HelpRequested);
        Assert.AreEqual("something", result.ValueOf<string>(name));
    }

    // Issue #54. The first Build used to add the help option to the builder's own list, so
    // the second found the name taken and built a schema whose "--help" did nothing.
    [TestMethod]
    public void Build_CalledTwice_GivesTwoSchemasThatBothRecognizeHelp()
    {
        IArgumentSchemaBuilder builder =
            ArgumentSchema.Create().Option<string>("--name", required: true);

        ArgumentSchema first = builder.Build();
        ArgumentSchema second = builder.Build();

        Assert.AreEqual(first.Options.Count, second.Options.Count);
        Assert.IsTrue(first.Parse("--help").HelpRequested);
        Assert.IsTrue(second.Parse("--help").HelpRequested);
        Assert.AreEqual(first.HelpText(), second.HelpText());
    }

    #endregion

    #region Defaults

    // Issue #53. Every int option used to show "(default: 0)".
    [TestMethod]
    public void HelpText_ValueTypedOptionWithNoDefault_DoesNotClaimOne()
    {
        string helpText =
            ArgumentSchema.Create()
                .Option<int>("--count", description: "How many")
                .Option<EmployeeType>("--type", description: "Which")
                .Build()
                .HelpText();

        Assert.IsFalse(helpText.Contains("default"), helpText);
    }

    #endregion
}
