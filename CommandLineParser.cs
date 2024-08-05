using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CommandLine
{
    /// <summary>
    /// Provides an command-line interpreter.
    /// </summary>
    public class CommandLineParser
    {
        /// <summary>
        /// Gets or sets the short form prefix string used on arguments.
        /// </summary>
        public string ShortFormPrefix { get; set; } = "-";

        /// <summary>
        /// Gets or sets the long form prefix string used on arguments.
        /// </summary>
        public string LongFormPrefix { get; set; } = "--";

        /// <summary>
        /// Splits the specified command-line string.
        /// </summary>
        /// <param name="commandLine">The command line string.</param>
        public static string[] Split(string commandLine)
        {
            bool doubleQuotes = false;
            bool singleQuotes = false;
            bool escaping = false;

            List<string> parts = new List<string>();
            StringBuilder mounting = new StringBuilder();

            using (StringReader sr = new StringReader(commandLine))
            {
                int c;
                while ((c = sr.Read()) >= 0)
                {
                    char current = (char)c;

                    if (escaping)
                    {
                        if (current != '\n' && current != '\r')
                            mounting.Append(current);

                        escaping = false;
                        continue;
                    }

                    if (char.IsWhiteSpace(current) && !doubleQuotes && !singleQuotes)
                    {
                        if (mounting.Length > 0)
                            parts.Add(mounting.ToString());
                        mounting.Clear();
                    }
                    else if (current == '\'' && !doubleQuotes)
                    {
                        singleQuotes = !singleQuotes;
                    }
                    else if (current == '"' && !singleQuotes)
                    {
                        doubleQuotes = !doubleQuotes;
                    }
                    else if (current == '^' || current == '\\')
                    {
                        escaping = true;
                    }
                    else
                    {
                        mounting.Append(current);
                    }
                }

                if (mounting.Length > 0)
                    parts.Add(mounting.ToString());
            }

            return parts.ToArray();
        }

        /// <summary>
        /// Gets the input arguments used to construct this interpreter.
        /// </summary>
        public string[] Arguments { get; }

        /// <summary>
        /// Gets or sets the <see cref="System.StringComparison"/> used for verb comparisons.
        /// </summary>
        public StringComparison Comparer { get; set; } = StringComparison.InvariantCultureIgnoreCase;

        /// <summary>
        /// Creates an new instance of the <see cref="CommandLineParser"/> with given command-line parameters.
        /// </summary>
        /// <param name="args">An array of command-line parameters.</param>
        public CommandLineParser(string[] args)
        {
            Arguments = args;
        }

        /// <summary>
        /// Creates an new instance of the <see cref="CommandLineParser"/> with given command-line parameters.
        /// </summary>
        /// <param name="args">An array of command-line parameters.</param>
        /// <param name="comparer">Represents the <see cref="System.StringComparison"/> used for verb comparisons.</param>
        public CommandLineParser(string[] args, StringComparison comparer)
        {
            Arguments = args;
            Comparer = comparer;
        }

        /// <summary>
        /// Returns all values that are part of the specified switch. The search searches all preceding values of the
        /// specified verb, and stops in the presence of an antecedent verb.
        /// </summary>
        /// <param name="longVerb">The verb in it's long form.</param>
        /// <param name="shortVerb">The verb in it's short form.</param>
        public IEnumerable<string> GetValues(string longVerb, char? shortVerb = null)
        {
            bool insertingNext = false;
            for (int i = 0; i < Arguments.Length; i++)
            {
                string item = Arguments[i];
                if (insertingNext)
                {
                    if (item.StartsWith(ShortFormPrefix))
                    {
                        insertingNext = false;
                    }
                    else
                    {
                        yield return item;
                    }
                }

                if (IsVerbMatch(item, longVerb, shortVerb))
                {
                    insertingNext = true;
                }
            }
        }

        /// <summary>
        /// Returns the first value found that is preceded by the specified verb.
        /// </summary>
        /// <param name="longVerb">The verb in it's long form.</param>
        /// <param name="shortVerb">The verb in it's short form.</param>
        /// <returns>The value preceded by the specified verb, or null if the verb is not defined or has no value.</returns>
        public string? GetValue(string longVerb, char? shortVerb = null)
        {
            for (int i = 0; i < Arguments.Length; i++)
            {
                string item = Arguments[i];

                if (IsVerbMatch(item, longVerb, shortVerb))
                {
                    return Lookup(i + 1);
                }
            }
            return null;
        }

        /// <summary>
        /// Returns an <see cref="bool"/> indicating whether the specified verb is present in the argument collection.
        /// </summary>
        /// <param name="longVerb">The verb in it's long form.</param>
        /// <param name="shortVerb">The verb in it's short form.</param>
        /// <returns>An <see cref="bool"/> indicating whether the specified verb is present in the argument collection.</returns>
        public bool IsDefined(string longVerb, char? shortVerb = null)
        {
            for (int i = 0; i < Arguments.Length; i++)
            {
                string item = Arguments[i];

                if (IsVerbMatch(item, longVerb, shortVerb))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns all values that are not verbs and that are not preceded by a verb.
        /// </summary>
        public IEnumerable<string> GetRemainder()
        {
            for (int i = 0; i < Arguments.Length; i++)
            {
                string? before = i == 0 ? null : Arguments[i - 1];
                string item = Arguments[i];

                bool beforeIsSwitch = before != null && before.StartsWith(ShortFormPrefix);

                if (!beforeIsSwitch && !item.StartsWith(ShortFormPrefix))
                {
                    yield return item;
                }
            }
        }

        bool IsVerbMatch(string verb, string longForm, char? shortForm)
        {
            if (verb.StartsWith(LongFormPrefix + longForm, Comparer)) return true;
            if (shortForm is char s && verb.StartsWith(ShortFormPrefix + s, Comparer)) return true;
            return false;
        }

        string? Lookup(int index)
        {
            if (index >= 0 && index < Arguments.Length)
                return Arguments[index];
            return null;
        }
    }
}