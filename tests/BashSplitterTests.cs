using CommandLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandLine.Tests;

[TestClass]
public class BashSplitterTests
{
    static string[] Split(string cmd) => CommandLineParser.Split(cmd, CommandLineSplitFormat.Bash);

    [TestMethod]
    public void Empty_ReturnsEmptyArray()
        => CollectionAssert.AreEqual(Array.Empty<string>(), Split(""));

    [TestMethod]
    public void WhitespaceOnly_ReturnsEmptyArray()
        => CollectionAssert.AreEqual(Array.Empty<string>(), Split("   "));

    [TestMethod]
    public void TabOnly_ReturnsEmptyArray()
        => CollectionAssert.AreEqual(Array.Empty<string>(), Split("\t\t"));

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
        => CollectionAssert.AreEqual(new[] { "hello world" }, Split("\"hello world\""));

    [TestMethod]
    public void SingleQuotes_PreservesInternalSpaces()
        => CollectionAssert.AreEqual(new[] { "hello world" }, Split("'hello world'"));

    [TestMethod]
    public void EmptyDoubleQuotes_ProducesEmptyArg()
        => CollectionAssert.AreEqual(new[] { "" }, Split("\"\""));

    [TestMethod]
    public void EmptySingleQuotes_ProducesEmptyArg()
        => CollectionAssert.AreEqual(new[] { "" }, Split("''"));

    [TestMethod]
    public void AdjacentQuotedAndUnquoted_Merge()
        => CollectionAssert.AreEqual(new[] { "hello" }, Split("hel\"lo\""));

    [TestMethod]
    public void AdjacentSingleAndDoubleQuotes_Merge()
        => CollectionAssert.AreEqual(new[] { "foobar" }, Split("'foo'\"bar\""));

    [TestMethod]
    public void BackslashSpace_OutsideQuotes_EscapesSpace()
        // "hello\ world" -> single arg: "hello world"
        => CollectionAssert.AreEqual(new[] { "hello world" }, Split(@"hello\ world"));

    [TestMethod]
    public void BackslashNewline_OutsideQuotes_LineContinuation()
        // "hello\<LF>world" -> single arg: "helloworld"
        => CollectionAssert.AreEqual(new[] { "helloworld" }, Split("hello\\\nworld"));

    [TestMethod]
    public void BackslashAtEnd_IsLiteralBackslash()
        => CollectionAssert.AreEqual(new[] { "hello\\" }, Split(@"hello\"));

    [TestMethod]
    public void DoubleBackslash_OutsideQuotes_SingleBackslash()
        => CollectionAssert.AreEqual(new[] { "\\" }, Split(@"\\"));

    [TestMethod]
    public void BackslashDollar_InsideDoubleQuotes_EscapesDollar()
        => CollectionAssert.AreEqual(new[] { "$" }, Split("\"\\$\""));

    [TestMethod]
    public void BackslashDoubleQuote_InsideDoubleQuotes_EscapesQuote()
        => CollectionAssert.AreEqual(new[] { "\"" }, Split("\"\\\"\""));

    [TestMethod]
    public void BackslashBackslash_InsideDoubleQuotes_EscapesBackslash()
        => CollectionAssert.AreEqual(new[] { "\\" }, Split("\"\\\\\""));

    [TestMethod]
    public void BackslashNonSpecial_InsideDoubleQuotes_PreservesBothChars()
        // "\a" inside double quotes -> both chars preserved: \a
        => CollectionAssert.AreEqual(new[] { "\\a" }, Split("\"\\a\""));

    [TestMethod]
    public void SingleQuotes_PreservesBackslashLiterally()
        // 'a\b' -> a\b (no escape processing inside single quotes)
        => CollectionAssert.AreEqual(new[] { @"a\b" }, Split(@"'a\b'"));

    [TestMethod]
    public void SingleQuotes_PreservesSpecialCharsLiterally()
        // '$VAR \n "test"' (with literal backslash-n, not a newline)
        => CollectionAssert.AreEqual(new[] { "$VAR \\n \"test\"" }, Split("'$VAR \\n \"test\"'"));

    [TestMethod]
    public void SingleQuotes_DoubleQuotesInsideArePreservedLiterally()
        => CollectionAssert.AreEqual(new[] { "\"test\"" }, Split("'\"test\"'"));

    [TestMethod]
    public void CrLf_NormalizedToLf()
        => CollectionAssert.AreEqual(new[] { "a", "b" }, Split("a\r\nb"));

    [TestMethod]
    public void MultipleArgs_WithMixedQuoted()
        => CollectionAssert.AreEqual(new[] { "foo bar", "baz" }, Split("\"foo bar\" baz"));

    [TestMethod]
    public void LeadingAndTrailingSpaces_AreTrimmed()
        => CollectionAssert.AreEqual(new[] { "hello" }, Split("  hello  "));
}
