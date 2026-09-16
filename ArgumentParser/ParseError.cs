namespace ArgumentParser
{
    /// <summary>
    /// One problem found while parsing against a schema. A parse collects every error rather
    /// than stopping at the first, because a command line application usually wants to print
    /// all of them at once instead of making the user fix them one run at a time.
    /// </summary>
    public class ParseError
    {
        /// <summary>
        /// What kind of problem this is.
        /// </summary>
        public ParseErrorKind Kind { get; }

        /// <summary>
        /// The option this concerns. For an unknown option this is the name as it was typed.
        /// </summary>
        public string OptionName { get; }

        /// <summary>
        /// The offending value, where there was one. Null for a missing or unknown option.
        /// </summary>
        public string? Value { get; }

        /// <summary>
        /// A message suitable for showing to the person who typed the command.
        /// </summary>
        public string Message { get; }

        internal ParseError(ParseErrorKind kind, string optionName, string? value, string message)
        {
            Kind = kind;
            OptionName = optionName;
            Value = value;
            Message = message;
        }

        /// <summary>
        /// Returns the message, so an error can be written straight to output.
        /// </summary>
        public override string ToString() => Message;
    }
}
