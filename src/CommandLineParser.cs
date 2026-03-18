using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace CommandLine
{
    /// <summary>
    /// Provides an command-line interpreter.
    /// </summary>
    public sealed class CommandLineParser
    {

        /// <summary>
        /// Splits a command line string into an array of substrings based on the specified format.
        /// </summary>
        /// <param name="commandLine">The command line string to split.</param>
        /// <param name="format">The format to use for splitting the command line. Defaults to <see cref="CommandLineSplitFormat.AutoDetect"/>.</param>
        /// <returns>An array of substrings representing the split command line.</returns>
        public static string[] Split(string commandLine, CommandLineSplitFormat format = CommandLineSplitFormat.AutoDetect)
        {
            if (format == CommandLineSplitFormat.Bash)
            {
                return BashCommandLineParser.Split(commandLine);
            }
            else if (format == CommandLineSplitFormat.Windows)
            {
                return WindowsCommandLineParser.Split(commandLine);
            }
            else
            {
                return AutoDetectCommandLineParser.Split(commandLine);
            }
        }

        /// <summary>
        /// Parses a command line string into a <see cref="CommandLineParser"/> instance.
        /// </summary>
        /// <param name="commandLine">The command line string to parse.</param>
        /// <param name="format">The format to use for parsing the command line. Defaults to <see cref="CommandLineSplitFormat.AutoDetect"/>.</param>
        /// <returns>A <see cref="CommandLineParser"/> instance representing the parsed command line.</returns>
        public static CommandLineParser Parse(string commandLine, CommandLineSplitFormat format = CommandLineSplitFormat.AutoDetect)
        {
            string[] split = Split(commandLine, format);
            return new CommandLineParser(split);
        }

        /// <summary>
        /// Gets or sets the short form prefix string used on arguments.
        /// </summary>
        public string ShortFormPrefix { get; set; } = "-";

        /// <summary>
        /// Gets or sets the long form prefix string used on arguments.
        /// </summary>
        public string LongFormPrefix { get; set; } = "--";

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
        /// Gets the value at the specified index.
        /// </summary>  
        /// <param name="index">The index of the value.</param>
        public string? GetValue(int index)
        {
            if (index >= 0 && index < Arguments.Length)
                return Arguments[index];
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

        #region Helpers

        /// <summary>
        /// Returns the value of the specified verb as a 16-bit signed integer.
        /// </summary>
        /// <param name="longVerb">The verb in it's long form.</param>
        /// <param name="shortVerb">The verb in it's short form.</param>
        /// <param name="formatProvider">The <see cref="IFormatProvider"/> to use for parsing the value.</param>
        /// <returns>The value of the specified verb as a 16-bit signed integer, or null if the verb is not defined or has no value.</returns>
        public Int16? GetInt16(string longVerb, char? shortVerb = null, IFormatProvider? formatProvider = default)
        {
            if (Int16.TryParse(GetValue(longVerb, shortVerb), NumberStyles.Any, formatProvider, out var value))
            {
                return value;
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Returns the value of the specified verb as a 32-bit signed integer.
        /// </summary>
        /// <param name="longVerb">The verb in it's long form.</param>
        /// <param name="shortVerb">The verb in it's short form.</param>
        /// <param name="formatProvider">The <see cref="IFormatProvider"/> to use for parsing the value.</param>
        /// <returns>The value of the specified verb as a 32-bit signed integer, or null if the verb is not defined or has no value.</returns>
        public Int32? GetInt32(string longVerb, char? shortVerb = null, IFormatProvider? formatProvider = default)
        {
            if (Int32.TryParse(GetValue(longVerb, shortVerb), NumberStyles.Any, formatProvider, out var value))
            {
                return value;
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Returns the value of the specified verb as a 64-bit signed integer.
        /// </summary>
        /// <param name="longVerb">The verb in it's long form.</param>
        /// <param name="shortVerb">The verb in it's short form.</param>
        /// <param name="formatProvider">The <see cref="IFormatProvider"/> to use for parsing the value.</param>
        /// <returns>The value of the specified verb as a 64-bit signed integer, or null if the verb is not defined or has no value.</returns>
        public Int64? GetInt64(string longVerb, char? shortVerb = null, IFormatProvider? formatProvider = default)
        {
            if (Int64.TryParse(GetValue(longVerb, shortVerb), NumberStyles.Any, formatProvider, out var value))
            {
                return value;
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Returns the value of the specified verb as a 16-bit unsigned integer.
        /// </summary>
        /// <param name="longVerb">The verb in it's long form.</param>
        /// <param name="shortVerb">The verb in it's short form.</param>
        /// <param name="formatProvider">The <see cref="IFormatProvider"/> to use for parsing the value.</param>
        /// <returns>The value of the specified verb as a 16-bit unsigned integer, or null if the verb is not defined or has no value.</returns>
        public UInt16? GetUInt16(string longVerb, char? shortVerb = null, IFormatProvider? formatProvider = default)
        {
            if (UInt16.TryParse(GetValue(longVerb, shortVerb), NumberStyles.Any, formatProvider, out var value))
            {
                return value;
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Returns the value of the specified verb as a 32-bit unsigned integer.
        /// </summary>
        /// <param name="longVerb">The verb in it's long form.</param>
        /// <param name="shortVerb">The verb in it's short form.</param>
        /// <param name="formatProvider">The <see cref="IFormatProvider"/> to use for parsing the value.</param>
        /// <returns>The value of the specified verb as a 32-bit unsigned integer, or null if the verb is not defined or has no value.</returns>
        public UInt32? GetUInt32(string longVerb, char? shortVerb = null, IFormatProvider? formatProvider = default)
        {
            if (UInt32.TryParse(GetValue(longVerb, shortVerb), NumberStyles.Any, formatProvider, out var value))
            {
                return value;
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Returns the value of the specified verb as a 64-bit unsigned integer.
        /// </summary>
        /// <param name="longVerb">The verb in it's long form.</param>
        /// <param name="shortVerb">The verb in it's short form.</param>
        /// <param name="formatProvider">The <see cref="IFormatProvider"/> to use for parsing the value.</param>
        /// <returns>The value of the specified verb as a 64-bit unsigned integer, or null if the verb is not defined or has no value.</returns>
        public UInt64? GetUInt64(string longVerb, char? shortVerb = null, IFormatProvider? formatProvider = default)
        {
            if (UInt64.TryParse(GetValue(longVerb, shortVerb), NumberStyles.Any, formatProvider, out var value))
            {
                return value;
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Returns the value of the specified verb as a single-precision floating-point number.
        /// </summary>
        /// <param name="longVerb">The verb in it's long form.</param>
        /// <param name="shortVerb">The verb in it's short form.</param>
        /// <param name="formatProvider">The <see cref="IFormatProvider"/> to use for parsing the value.</param>
        /// <returns>The value of the specified verb as a single-precision floating-point number, or null if the verb is not defined or has no value.</returns>
        public float? GetFloat(string longVerb, char? shortVerb = null, IFormatProvider? formatProvider = default)
        {
            if (float.TryParse(GetValue(longVerb, shortVerb), NumberStyles.Any, formatProvider, out var value))
            {
                return value;
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Returns the value of the specified verb as a decimal number.
        /// </summary>
        /// <param name="longVerb">The verb in it's long form.</param>
        /// <param name="shortVerb">The verb in it's short form.</param>
        /// <param name="formatProvider">The <see cref="IFormatProvider"/> to use for parsing the value.</param>
        /// <returns>The value of the specified verb as a decimal number, or null if the verb is not defined or has no value.</returns>
        public decimal? GetDecimal(string longVerb, char? shortVerb = null, IFormatProvider? formatProvider = default)
        {
            if (decimal.TryParse(GetValue(longVerb, shortVerb), NumberStyles.Any, formatProvider, out var value))
            {
                return value;
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Returns the value of the specified verb as an unsigned byte.
        /// </summary>
        /// <param name="longVerb">The verb in it's long form.</param>
        /// <param name="shortVerb">The verb in it's short form.</param>
        /// <param name="formatProvider">The <see cref="IFormatProvider"/> to use for parsing the value.</param>
        /// <returns>The value of the specified verb as an unsigned byte, or null if the verb is not defined or has no value.</returns>
        public byte? GetByte(string longVerb, char? shortVerb = null, IFormatProvider? formatProvider = default)
        {
            if (byte.TryParse(GetValue(longVerb, shortVerb), NumberStyles.Any, formatProvider, out var value))
            {
                return value;
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Returns the value of the specified verb as a globally unique identifier (GUID).
        /// </summary>
        /// <param name="longVerb">The verb in it's long form.</param>
        /// <param name="shortVerb">The verb in it's short form.</param>
        /// <returns>The value of the specified verb as a GUID, or null if the verb is not defined or has no value.</returns>
        public Guid? GetGuid(string longVerb, char? shortVerb = null)
        {
            if (Guid.TryParse(GetValue(longVerb, shortVerb), out var value))
            {
                return value;
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Returns the value of the specified verb as a file path.
        /// </summary>
        /// <param name="longVerb">The verb in its long form.</param>
        /// <param name="shortVerb">The verb in its short form.</param>
        /// <returns>The value of the specified verb as a file path, or null if the verb is not defined or has no value.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the file does not exist and the value is not null or empty.</exception>
        public string? GetFilePath(string longVerb, char? shortVerb = null)
        {
            var filePath = GetValue(longVerb, shortVerb);
            if (string.IsNullOrEmpty(filePath))
            {
                return default;
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The specified file \"{filePath}\" was not found.");
            }

            return Path.GetFullPath(filePath);
        }

        /// <summary>
        /// Returns the value of the specified verb as a directory path.
        /// </summary>
        /// <param name="longVerb">The verb in its long form.</param>
        /// <param name="shortVerb">The verb in its short form.</param>
        /// <returns>The value of the specified verb as a directory path, or null if the verb is not defined or has no value.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist and the value is not null or empty.</exception>
        public string? GetDirectoryPath(string longVerb, char? shortVerb = null)
        {
            var directoryPath = GetValue(longVerb, shortVerb);
            if (string.IsNullOrEmpty(directoryPath))
            {
                return default;
            }

            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"The specified directory \"{directoryPath}\" was not found.");
            }

            return Path.GetFullPath(directoryPath);
        }
        #endregion

        bool IsVerbMatch(string verb, string longForm, char? shortForm)
        {
            if (verb.StartsWith(LongFormPrefix + longForm, Comparer))
                return true;
            if (shortForm is char s && verb.StartsWith(ShortFormPrefix + s, Comparer))
                return true;
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