using System.Globalization;
using System.IO;
using CommandLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandLine.Tests;

[TestClass]
public class CommandLineParserApiTests
{
    static CommandLineParser Make(params string[] args) => new CommandLineParser(args);

    // --- Parse() / Split() static methods ---

    [TestMethod]
    public void Parse_ReturnsParserWithCorrectArguments()
    {
        var parser = CommandLineParser.Parse("prog --flag value");
        Assert.AreEqual(3, parser.Arguments.Length);
        Assert.AreEqual("prog", parser.Arguments[0]);
    }

    [TestMethod]
    public void Split_ReturnsCorrectArray()
    {
        var result = CommandLineParser.Split("a b c");
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, result);
    }

    // --- GetValue(longVerb) ---

    [TestMethod]
    public void GetValue_LongVerb_Found_ReturnsValue()
    {
        var p = Make("--output", "file.txt");
        Assert.AreEqual("file.txt", p.GetValue("output"));
    }

    [TestMethod]
    public void GetValue_LongVerb_NotDefined_ReturnsNull()
    {
        var p = Make("--flag");
        Assert.IsNull(p.GetValue("output"));
    }

    [TestMethod]
    public void GetValue_LongVerb_DefinedButNoValue_ReturnsNull()
    {
        var p = Make("--output");
        Assert.IsNull(p.GetValue("output"));
    }

    [TestMethod]
    public void GetValue_ShortVerb_Found_ReturnsValue()
    {
        var p = Make("-o", "file.txt");
        Assert.AreEqual("file.txt", p.GetValue("output", 'o'));
    }

    [TestMethod]
    public void GetValue_ShortVerb_NotDefined_ReturnsNull()
    {
        var p = Make("-x", "file.txt");
        Assert.IsNull(p.GetValue("output", 'o'));
    }

    [TestMethod]
    public void GetValue_LongVerb_MatchesFirst_WhenMultipleOccurrences()
    {
        var p = Make("--output", "first.txt", "--output", "second.txt");
        Assert.AreEqual("first.txt", p.GetValue("output"));
    }

    [TestMethod]
    public void GetValue_LongVerbPrefixMatch_MatchesPartialVerb()
    {
        // "output-file" starts with "output", so GetValue("output") matches it
        var p = Make("--output-file", "foo.txt");
        Assert.AreEqual("foo.txt", p.GetValue("output"));
    }

    // --- GetValue(int index) ---

    [TestMethod]
    public void GetValue_ByIndex_ValidIndex_ReturnsArg()
    {
        var p = Make("prog", "--flag", "value");
        Assert.AreEqual("prog", p.GetValue(0));
        Assert.AreEqual("--flag", p.GetValue(1));
        Assert.AreEqual("value", p.GetValue(2));
    }

    [TestMethod]
    public void GetValue_ByIndex_NegativeIndex_ReturnsNull()
        => Assert.IsNull(Make("prog").GetValue(-1));

    [TestMethod]
    public void GetValue_ByIndex_BeyondLength_ReturnsNull()
        => Assert.IsNull(Make("prog").GetValue(5));

    // --- IsDefined ---

    [TestMethod]
    public void IsDefined_LongVerb_Present_ReturnsTrue()
        => Assert.IsTrue(Make("--verbose").IsDefined("verbose"));

    [TestMethod]
    public void IsDefined_LongVerb_Absent_ReturnsFalse()
        => Assert.IsFalse(Make("--flag").IsDefined("verbose"));

    [TestMethod]
    public void IsDefined_ShortVerb_Present_ReturnsTrue()
        => Assert.IsTrue(Make("-v").IsDefined("verbose", 'v'));

    [TestMethod]
    public void IsDefined_ShortVerb_Absent_ReturnsFalse()
        => Assert.IsFalse(Make("-x").IsDefined("verbose", 'v'));

    [TestMethod]
    public void IsDefined_EmptyArgs_ReturnsFalse()
        => Assert.IsFalse(Make().IsDefined("flag"));

    // --- GetValues ---

    [TestMethod]
    public void GetValues_MultipleValues_ReturnsAll()
    {
        var p = Make("--file", "a.txt", "b.txt", "c.txt");
        CollectionAssert.AreEqual(new[] { "a.txt", "b.txt", "c.txt" }, p.GetValues("file").ToArray());
    }

    [TestMethod]
    public void GetValues_StopsAtNextVerb()
    {
        var p = Make("--files", "a.txt", "b.txt", "--flag");
        CollectionAssert.AreEqual(new[] { "a.txt", "b.txt" }, p.GetValues("files").ToArray());
    }

    [TestMethod]
    public void GetValues_NotDefined_ReturnsEmpty()
        => Assert.AreEqual(0, Make("--flag").GetValues("files").Count());

    [TestMethod]
    public void GetValues_MultipleOccurrences_CollectsFromAll()
    {
        var p = Make("--file", "a.txt", "--file", "b.txt");
        CollectionAssert.AreEqual(new[] { "a.txt", "b.txt" }, p.GetValues("file").ToArray());
    }

    // --- GetRemainder ---

    [TestMethod]
    public void GetRemainder_ReturnsNonVerbArgs()
    {
        var p = Make("cmd", "--flag", "value");
        CollectionAssert.AreEqual(new[] { "cmd" }, p.GetRemainder().ToArray());
    }

    [TestMethod]
    public void GetRemainder_MultipleRemainders()
    {
        var p = Make("sub", "command", "--flag", "val");
        CollectionAssert.AreEqual(new[] { "sub", "command" }, p.GetRemainder().ToArray());
    }

    [TestMethod]
    public void GetRemainder_AllSwitches_ReturnsEmpty()
        => Assert.AreEqual(0, Make("--a", "x", "--b", "y").GetRemainder().Count());

    [TestMethod]
    public void GetRemainder_TrailingPositional()
    {
        var p = Make("--output", "file.txt", "input.txt");
        CollectionAssert.AreEqual(new[] { "input.txt" }, p.GetRemainder().ToArray());
    }

    // --- Numeric helpers ---

    [TestMethod]
    public void GetInt32_ValidValue_ReturnsInt()
    {
        var p = Make("--count", "42");
        Assert.AreEqual(42, p.GetInt32("count"));
    }

    [TestMethod]
    public void GetInt32_NotDefined_ReturnsNull()
        => Assert.IsNull(Make("--flag").GetInt32("count"));

    [TestMethod]
    public void GetInt32_InvalidString_ReturnsNull()
        => Assert.IsNull(Make("--count", "abc").GetInt32("count"));

    [TestMethod]
    public void GetInt64_ValidValue_ReturnsLong()
    {
        var p = Make("--size", "9999999999");
        Assert.AreEqual(9999999999L, p.GetInt64("size"));
    }

    [TestMethod]
    public void GetInt16_ValidValue_ReturnsShort()
    {
        var p = Make("--port", "8080");
        Assert.AreEqual((short)8080, p.GetInt16("port"));
    }

    [TestMethod]
    public void GetFloat_ValidValue_ReturnsFloat()
    {
        var p = Make("--ratio", "3.14");
        Assert.IsNotNull(p.GetFloat("ratio", formatProvider: CultureInfo.InvariantCulture));
        Assert.AreEqual(3.14f, p.GetFloat("ratio", formatProvider: CultureInfo.InvariantCulture)!.Value, 0.001f);
    }

    [TestMethod]
    public void GetDecimal_ValidValue_ReturnsDecimal()
    {
        var p = Make("--amount", "9.99");
        Assert.AreEqual(9.99m, p.GetDecimal("amount", formatProvider: CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void GetByte_ValidValue_ReturnsByte()
    {
        var p = Make("--level", "255");
        Assert.AreEqual((byte)255, p.GetByte("level"));
    }

    [TestMethod]
    public void GetUInt32_ValidValue_ReturnsUInt()
    {
        var p = Make("--id", "4294967295");
        Assert.AreEqual(4294967295u, p.GetUInt32("id"));
    }

    [TestMethod]
    public void GetUInt16_ValidValue_ReturnsUShort()
    {
        var p = Make("--port", "65535");
        Assert.AreEqual((ushort)65535, p.GetUInt16("port"));
    }

    [TestMethod]
    public void GetUInt64_ValidValue_ReturnsULong()
    {
        var p = Make("--size", "18446744073709551615");
        Assert.AreEqual(ulong.MaxValue, p.GetUInt64("size"));
    }

    [TestMethod]
    public void GetGuid_ValidValue_ReturnsGuid()
    {
        var guid = Guid.NewGuid();
        var p = Make("--id", guid.ToString());
        Assert.AreEqual(guid, p.GetGuid("id"));
    }

    [TestMethod]
    public void GetGuid_InvalidValue_ReturnsNull()
        => Assert.IsNull(Make("--id", "not-a-guid").GetGuid("id"));

    // --- File / directory helpers ---

    [TestMethod]
    public void GetFilePath_NotDefined_ReturnsNull()
        => Assert.IsNull(Make("--flag").GetFilePath("file"));

    [TestMethod]
    public void GetFilePath_FileNotFound_Throws()
    {
        var p = Make("--file", @"C:\nonexistent\path\file.txt");
        Assert.ThrowsException<FileNotFoundException>(() => p.GetFilePath("file"));
    }

    [TestMethod]
    public void GetFilePath_ExistingFile_ReturnsFullPath()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            var p = Make("--file", tempFile);
            string? result = p.GetFilePath("file");
            Assert.IsNotNull(result);
            Assert.AreEqual(Path.GetFullPath(tempFile), result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void GetDirectoryPath_NotDefined_ReturnsNull()
        => Assert.IsNull(Make("--flag").GetDirectoryPath("dir"));

    [TestMethod]
    public void GetDirectoryPath_DirectoryNotFound_Throws()
    {
        var p = Make("--dir", @"C:\nonexistent\path\to\dir");
        Assert.ThrowsException<DirectoryNotFoundException>(() => p.GetDirectoryPath("dir"));
    }

    [TestMethod]
    public void GetDirectoryPath_ExistingDirectory_ReturnsFullPath()
    {
        string tempDir = Path.GetTempPath();
        var p = Make("--dir", tempDir);
        string? result = p.GetDirectoryPath("dir");
        Assert.IsNotNull(result);
        Assert.AreEqual(Path.GetFullPath(tempDir), result);
    }

    // --- Custom prefixes and comparer ---

    [TestMethod]
    public void CustomLongFormPrefix_MatchesVerb()
    {
        var p = new CommandLineParser(new[] { "/output", "file.txt" })
        {
            ShortFormPrefix = "/",
            LongFormPrefix = "/"
        };
        Assert.AreEqual("file.txt", p.GetValue("output"));
    }

    [TestMethod]
    public void CaseSensitiveComparer_DoesNotMatchDifferentCase()
    {
        var p = new CommandLineParser(new[] { "--Output", "file.txt" }, StringComparison.Ordinal);
        Assert.IsNull(p.GetValue("output"));
    }

    [TestMethod]
    public void DefaultComparer_IsCaseInsensitive()
    {
        var p = Make("--OUTPUT", "file.txt");
        Assert.AreEqual("file.txt", p.GetValue("output"));
    }
}
