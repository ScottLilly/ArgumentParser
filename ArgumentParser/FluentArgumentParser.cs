using System.Collections.Generic;
using System.Linq;

namespace ArgumentParser
{
    public class FluentArgumentParser : IFluentArgumentParserBuilder
    {
        #region Private variables

        // User HashSets for storing separators, to avoid duplicates
        private readonly HashSet<string> _argumentSeparators = new HashSet<string>();
        private readonly HashSet<char> _keyValueSeparators = new HashSet<char>();

        #endregion

        #region Instantiating method

        public static IFluentArgumentParserBuilder Create()
        {
            return new FluentArgumentParser();
        }

        #endregion

        #region Chaining methods

        public IFluentArgumentParserBuilder AddArgumentSeparator(char argumentSeparator)
        {
            _argumentSeparators.Add(argumentSeparator.ToString());
            
            return this;
        }

        public IFluentArgumentParserBuilder AddArgumentSeparator(string argumentSeparator)
        {
            _argumentSeparators.Add(argumentSeparator);

            return this;
        }

        public IFluentArgumentParserBuilder AddArgumentSeparators(char[] argumentSeparators)
        {
            foreach (char separator in argumentSeparators)
            {
                _argumentSeparators.Add(separator.ToString());
            }

            return this;
        }

        public IFluentArgumentParserBuilder AddArgumentSeparators(string[] argumentSeparator)
        {
            foreach (string separator in argumentSeparator)
            {
                _argumentSeparators.Add(separator);
            }

            return this;
        }

        public IFluentArgumentParserBuilder AddKeyValueSeparator(char keyValueSeparator)
        {
            _keyValueSeparators.Add(keyValueSeparator);

            return this;
        }

        public IFluentArgumentParserBuilder AddKeyValueSeparators(char[] keyValueSeparators)
        {
            foreach (char separator in keyValueSeparators)
            {
                _keyValueSeparators.Add(separator);
            }

            return this;
        }

        #endregion

        #region Execution methods

        public ParsedArguments Parse(string arguments)
        {
            Parser parser = new Parser(
                _argumentSeparators.ToArray(),
                _keyValueSeparators.ToArray());

            return parser.Parse(arguments);
        }

        public ParsedArguments Parse(string[] arguments)
        {
            Parser parser = new Parser(
                _argumentSeparators.ToArray(),
                _keyValueSeparators.ToArray());

            return parser.Parse(arguments);
        }

        #endregion
    }

    public interface IFluentArgumentParserBuilder
    {
        // Argument separators can be added as single characters or strings,
        // to allow for multi-character separators if needed.
        IFluentArgumentParserBuilder AddArgumentSeparator(char argumentSeparator);
        IFluentArgumentParserBuilder AddArgumentSeparators(char[] argumentSeparators);

        IFluentArgumentParserBuilder AddArgumentSeparator(string argumentSeparator);
        IFluentArgumentParserBuilder AddArgumentSeparators(string[] argumentSeparator);

        // Key/Value separators can only be added as single characters
        IFluentArgumentParserBuilder AddKeyValueSeparator(char keyValueSeparator);
        IFluentArgumentParserBuilder AddKeyValueSeparators(char[] keyValueSeparators);

        ParsedArguments Parse(string arguments);
        ParsedArguments Parse(string[] arguments);
    }
}
