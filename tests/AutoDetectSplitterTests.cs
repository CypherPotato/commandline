using CommandLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;

namespace CommandLine.Tests;

/// <summary>
/// AutoDetect uses heuristics to pick Bash or Windows parser.
/// Unambiguous Bash signals: single quotes, $VAR, ${VAR}, backslash+newline, backslash+space.
/// Unambiguous Windows signals: %VAR%, drive letter (X:\), UNC path (\\server).
/// Ambiguous or no-signal cases fall back to OS default (Windows on Windows, Bash on Unix).
/// </summary>
[TestClass]
public class AutoDetectSplitterTests
{
    static string[] Split(string cmd) => CommandLineParser.Split(cmd, CommandLineSplitFormat.AutoDetect);

    static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    [TestMethod]
    public void Empty_ReturnsEmptyArray()
        => CollectionAssert.AreEqual(Array.Empty<string>(), Split(""));

    [TestMethod]
    public void WhitespaceOnly_ReturnsEmptyArray()
        => CollectionAssert.AreEqual(Array.Empty<string>(), Split("   "));

    // --- Clear Bash detection ---

    [TestMethod]
    public void SingleQuote_DetectsBash_PreservesSpaces()
        // Single quote is a Bash-only signal; Windows parser would split on the space
        => CollectionAssert.AreEqual(new[] { "hello world" }, Split("'hello world'"));

    [TestMethod]
    public void BashVariable_Dollar_DetectsBash()
    {
        var result = Split("echo $PATH");
        CollectionAssert.AreEqual(new[] { "echo", "$PATH" }, result);
    }

    [TestMethod]
    public void BashVariable_DollarBrace_DetectsBash()
    {
        var result = Split("ls ${HOME}");
        CollectionAssert.AreEqual(new[] { "ls", "${HOME}" }, result);
    }

    [TestMethod]
    public void BashVariable_WithUnderscore_DetectsBash()
        => CollectionAssert.AreEqual(new[] { "echo", "$MY_VAR" }, Split("echo $MY_VAR"));

    [TestMethod]
    public void BackslashNewline_DetectsBash_ContinuesLine()
        // Backslash + newline is a Bash line-continuation signal
        => CollectionAssert.AreEqual(new[] { "helloworld" }, Split("hello\\\nworld"));

    [TestMethod]
    public void BackslashSpace_DetectsBash_EscapesSpace()
        // Backslash+space triggers hasBashStyleEscape but NOT hasProbablePathSeparator
        // (whitespace is excluded from the path-separator heuristic), so -> Bash
        => CollectionAssert.AreEqual(new[] { "run file" }, Split(@"run\ file"));

    [TestMethod]
    public void SingleQuote_WithDollar_DetectsBash()
        // Single quotes suppress expansion; the '$x' literal is preserved
        => CollectionAssert.AreEqual(new[] { "hello", "$x" }, Split("hello '$x'"));

    [TestMethod]
    public void MultipleBashVariables_DetectsBash()
        => CollectionAssert.AreEqual(new[] { "echo", "$FOO", "$BAR" }, Split("echo $FOO $BAR"));

    [TestMethod]
    public void SingleQuote_WithPath_DetectsBash()
        // Bash variable + single-quoted arg; no Windows signals (%VAR%, \path)
        => CollectionAssert.AreEqual(new[] { "$HOME", "/etc/hosts" }, Split("$HOME '/etc/hosts'"));

    // --- Clear Windows detection ---

    [TestMethod]
    public void WindowsVariable_DetectsWindows_NoBackslash()
        // %VAR% triggers Windows detection; no Bash signals present
        => CollectionAssert.AreEqual(new[] { "prog", "%TEMP%", "/flag" }, Split("prog %TEMP% /flag"));

    [TestMethod]
    public void WindowsVariable_Multiple_DetectsWindows()
        => CollectionAssert.AreEqual(new[] { "%VAR1%", "%VAR2%" }, Split("%VAR1% %VAR2%"));

    [TestMethod]
    public void WindowsVariable_Quoted_DetectsWindows()
    {
        string input = "prog \"output dir\" %CONFIG%";
        var result = Split(input);
        // Windows parser: quoted arg preserves spaces
        CollectionAssert.AreEqual(new[] { "prog", "output dir", "%CONFIG%" }, result);
    }

    // --- Neutral / OS-default cases ---
    // These produce identical results from both parsers, so assertions hold on any OS.

    [TestMethod]
    public void SimpleCommand_NoSpecialChars_ProducesCorrectSplit()
        => CollectionAssert.AreEqual(new[] { "cmd", "arg1", "arg2" }, Split("cmd arg1 arg2"));

    [TestMethod]
    public void DoubleQuotedArg_NoOtherSignals_PreservesSpaces()
        => CollectionAssert.AreEqual(new[] { "quoted arg" }, Split("\"quoted arg\""));

    [TestMethod]
    public void MultipleDoubleQuotedArgs_NoOtherSignals()
        => CollectionAssert.AreEqual(new[] { "a b", "c d" }, Split("\"a b\" \"c d\""));

    [TestMethod]
    public void SingleArg_NoSignals()
        => CollectionAssert.AreEqual(new[] { "hello" }, Split("hello"));

    // --- Ambiguous cases (both bash and windows signals) -> OS default ---
    // On Windows: Windows parser. On Unix: Bash parser.
    // We only assert that a non-null result with a reasonable count is returned.

    [TestMethod]
    public void Ambiguous_BothDollarAndPercent_ReturnsResult()
    {
        var result = Split("echo $HOME %TEMP%");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    [TestMethod]
    public void Ambiguous_DriveLetterAndDollarVar_ReturnsResult()
    {
        var result = Split("copy $src \"C:\\dest\"");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    [TestMethod]
    public void Ambiguous_WindowsPath_ReturnsResult()
    {
        // C:\path triggers both hasDriveLetter and hasBashStyleEscape (\p heuristic)
        var result = Split("prog \"C:\\Windows\\System32\" arg");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length >= 2);
    }

    [TestMethod]
    public void BashOnlySignal_SingleQuote_ResultDiffersFromWindows()
    {
        // Demonstrates that AutoDetect picks Bash, not Windows, for single-quoted input
        string[] autoResult = Split("'hello world'");
        string[] winResult = CommandLineParser.Split("'hello world'", CommandLineSplitFormat.Windows);
        // Bash: ["hello world"] (single arg); Windows: ["'hello", "world'"] (two args)
        CollectionAssert.AreNotEqual(winResult, autoResult);
        Assert.AreEqual(1, autoResult.Length);
    }

    [TestMethod]
    public void BashOnlySignal_BackslashSpace_ResultDiffersFromWindows()
    {
        string input = @"run\ file";
        string[] autoResult = Split(input);
        string[] winResult = CommandLineParser.Split(input, CommandLineSplitFormat.Windows);
        // Bash: ["run file"]; Windows: ["run\", "file"]
        CollectionAssert.AreNotEqual(winResult, autoResult);
        Assert.AreEqual(1, autoResult.Length);
    }
}
