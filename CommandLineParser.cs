using System;
using System.Collections.Generic;

namespace CommandLine
{
    /// <summary>
    /// Provides an command-line interpreter.
    /// </summary>
    public class CommandLineParser
    {
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
                    if (item.StartsWith("-"))
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

                bool beforeIsSwitch = before != null && before.StartsWith('-');

                if (!beforeIsSwitch && !item.StartsWith('-'))
                {
                    yield return item;
                }
            }
        }

        bool IsVerbMatch(string verb, string longForm, char? shortForm)
        {
            if (!verb.StartsWith('-')) return false;
            if (verb.StartsWith("--" + longForm, Comparer)) return true;
            if (shortForm is char c && verb.StartsWith("-" + shortForm, Comparer)) return true;
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