using CommandLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandLine.Tests;

[TestClass]
public class WindowsSplitterTests
{
    static string[] Split(string cmd) => CommandLineParser.Split(cmd, CommandLineSplitFormat.Windows);

    static readonly string BS = "\\";
    static readonly string Q = "\"";

    [TestMethod]
    public void Empty_ReturnsEmptyArray()
        => CollectionAssert.AreEqual(Array.Empty<string>(), Split(""));

    [TestMethod]
    public void WhitespaceOnly_ReturnsEmptyArray()
        => CollectionAssert.AreEqual(Array.Empty<string>(), Split("   "));

    [TestMethod]
    public void Tab_ReturnsEmptyArray()
        => CollectionAssert.AreEqual(Array.Empty<string>(), Split("\t"));

    [TestMethod]
    public void SingleArg_ReturnsSingle()
        => CollectionAssert.AreEqual(new[] { "hello" }, Split("hello"));

    [TestMethod]
    public void MultipleSimpleArgs_SplitBySpace()
        => CollectionAssert.AreEqual(new[] { "a", "b", "c" }, Split("a b c"));

    [TestMethod]
    public void MultipleSpaces_TreatedAsSingleSeparator()
        => CollectionAssert.AreEqual(new[] { "a", "b" }, Split("a   b"));

    [TestMethod]
    public void Tab_AsSeparator()
        => CollectionAssert.AreEqual(new[] { "a", "b" }, Split("a\tb"));

    [TestMethod]
    public void DoubleQuotes_PreservesInternalSpaces()
        => CollectionAssert.AreEqual(new[] { "hello world" }, Split(Q + "hello world" + Q));

    [TestMethod]
    public void EmptyDoubleQuotes_ProducesEmptyArg()
        => CollectionAssert.AreEqual(new[] { "" }, Split(Q + Q));

    [TestMethod]
    public void SingleQuotes_AreLiteral_NotSpecial()
        // Windows parser has no single-quote handling; ' is just a regular char
        => CollectionAssert.AreEqual(new[] { "'hello", "world'" }, Split("'hello world'"));

    [TestMethod]
    public void OneBackslash_NotBeforeQuote_IsLiteral()
        => CollectionAssert.AreEqual(new[] { @"hello\world" }, Split(@"hello\world"));

    [TestMethod]
    public void TwoBackslashes_NotBeforeQuote_AreLiteral()
        => CollectionAssert.AreEqual(new[] { @"\\" }, Split(@"\\"));

    [TestMethod]
    public void OddBackslashes_BeforeQuote_EscapesQuote()
    {
        // 1 backslash + quote -> literal " (0 backslashes output + literal quote)
        string input = BS + Q;
        CollectionAssert.AreEqual(new[] { Q }, Split(input));
    }

    [TestMethod]
    public void OddBackslashes_Three_BeforeQuote()
    {
        // 3 backslashes + quote -> 1 literal backslash + literal quote
        string input = BS + BS + BS + Q;
        CollectionAssert.AreEqual(new[] { BS + Q }, Split(input));
    }

    [TestMethod]
    public void EvenBackslashes_BeforeQuote_HalfLiteralAndQuoteToggle()
    {
        // 2 backslashes + "x" -> 1 literal backslash + "x" in quotes = \x
        string input = BS + BS + Q + "x" + Q;
        CollectionAssert.AreEqual(new[] { BS + "x" }, Split(input));
    }

    [TestMethod]
    public void EvenBackslashes_Four_BeforeQuote()
    {
        // 4 backslashes + "x" -> 2 literal backslashes + "x" quoted = \\x
        string input = BS + BS + BS + BS + Q + "x" + Q;
        CollectionAssert.AreEqual(new[] { BS + BS + "x" }, Split(input));
    }

    [TestMethod]
    public void QuoteToggle_MidToken_MergesSegments()
    {
        // hel"lo wor"ld -> "hello world"
        string input = "hel" + Q + "lo wor" + Q + "ld";
        CollectionAssert.AreEqual(new[] { "hello world" }, Split(input));
    }

    [TestMethod]
    public void AdjacentQuotedSegments_Merge()
    {
        // "hello""world" -> helloworld
        string input = Q + "hello" + Q + Q + "world" + Q;
        CollectionAssert.AreEqual(new[] { "helloworld" }, Split(input));
    }

    [TestMethod]
    public void MultipleArgs_WithQuotes()
    {
        string input = "prog " + Q + "path with spaces" + Q + " --flag";
        CollectionAssert.AreEqual(new[] { "prog", "path with spaces", "--flag" }, Split(input));
    }

    [TestMethod]
    public void LeadingAndTrailingSpaces_AreTrimmed()
        => CollectionAssert.AreEqual(new[] { "hello" }, Split("  hello  "));

    [TestMethod]
    public void BackslashInPath_IsPreservedLiterally()
        // No quote follows these backslashes, so they are literal
        => CollectionAssert.AreEqual(new[] { @"C:\Windows\System32" }, Split(@"C:\Windows\System32"));

    [TestMethod]
    public void UncPath_DoubleBackslash_IsPreservedLiterally()
        => CollectionAssert.AreEqual(new[] { @"\\server\share" }, Split(@"\\server\share"));

    [TestMethod]
    public void QuotedPath_WithBackslashes()
    {
        // "C:\Program Files\app" -> C:\Program Files\app
        string input = Q + @"C:\Program Files\app" + Q;
        CollectionAssert.AreEqual(new[] { @"C:\Program Files\app" }, Split(input));
    }

    [TestMethod]
    public void MultipleArgs_LastArgHasNoValue()
        => CollectionAssert.AreEqual(new[] { "prog", "arg1", "arg2" }, Split("prog arg1 arg2"));

    [TestMethod]
    public void QuotedArgFollowedByUnquoted_Merge()
    {
        // "foo"bar -> foobar
        string input = Q + "foo" + Q + "bar";
        CollectionAssert.AreEqual(new[] { "foobar" }, Split(input));
    }
}
