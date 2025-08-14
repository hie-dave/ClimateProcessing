using System.Runtime.Versioning;
using ClimateProcessing.Services;
using Xunit;

namespace ClimateProcessing.Tests.Services;

public sealed class ScriptWriterTests : IDisposable
{
    private readonly TempFile tempScript;

    public ScriptWriterTests()
    {
        tempScript = TempFile.Create(GetType().Name);
    }

    public void Dispose()
    {
        tempScript.Dispose();
    }

#if !WINDOWS
    [Fact]
    [UnsupportedOSPlatform("windows")]
    public void ScriptWriter_SetsCorrectFilePermissions_OnNonWindowsPlatforms()
    {
        // Initialise the script.
        using var writer = new ScriptWriter(tempScript.AbsolutePath);

        // Check file permissions
        UnixFileMode fileMode = File.GetUnixFileMode(tempScript.AbsolutePath);

        // Verify executable permissions are set.
        Assert.True(fileMode.HasFlag(UnixFileMode.UserExecute), "User execute permission not set by ScriptWriter");
        Assert.True(fileMode.HasFlag(UnixFileMode.GroupExecute), "Group execute permission not set by ScriptWriter");
        Assert.True(fileMode.HasFlag(UnixFileMode.OtherExecute), "Other execute permission not set by ScriptWriter");
    }
#endif

    [Theory]
    [InlineData("asdf")]
    [InlineData("#!/bin/bash", " ", "x", "y")]
    public async Task TestWriteAsync(params string[] content)
    {
        using (var writer = new ScriptWriter(tempScript.AbsolutePath))
        {
            foreach (string item in content)
                await writer.WriteAsync(item);
        }

        Assert.Equal(string.Join("", content), File.ReadAllText(tempScript.AbsolutePath));
    }

    [Theory]
    [InlineData("#!/usr/bin/env bash", "", "# comment at top of script")]
    [InlineData("line0", "line1", "line2")]
    public async Task TestWriteLineAsync(params string[] lines)
    {
        using (var writer = new ScriptWriter(tempScript.AbsolutePath))
        {
            foreach (string line in lines)
                await writer.WriteLineAsync(line);
        }

        // Should be LF (not CRLF), even on Windows.
        string expected = $"{string.Join("\n", lines)}\n";
        string actual = await File.ReadAllTextAsync(tempScript.AbsolutePath);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(64)]
    public async Task TestWriteEmptyLineAsync(int nlines)
    {
        using (var writer = new ScriptWriter(tempScript.AbsolutePath))
        {
            for (int i = 0; i < nlines; i++)
                await writer.WriteLineAsync();
        }

        string expected = new string('\n', nlines);
        string actual = await File.ReadAllTextAsync(tempScript.AbsolutePath);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task TestCompositeWriteAsync()
    {
        using (var writer = new ScriptWriter(tempScript.AbsolutePath))
        {
            await writer.WriteLineAsync("line0");
            await writer.WriteAsync("line1");
            await writer.WriteAsync("part2");
            await writer.WriteLineAsync();
            await writer.WriteAsync("line2");
            await writer.WriteLineAsync();
        }

        string expected = "line0\nline1part2\nline2\n";
        string actual = await File.ReadAllTextAsync(tempScript.AbsolutePath);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task TestDispose_ClosesWriter()
    {
        using (var writer = new ScriptWriter(tempScript.AbsolutePath))
        {
            await writer.WriteLineAsync("line0");

            // This should throw because the stream is still open.
            Assert.Throws<IOException>(() => File.OpenWrite(tempScript.AbsolutePath));
        }

        // Should be able to open the file for writing, as stream is now closed.
        using var _ = File.OpenWrite(tempScript.AbsolutePath);
    }
}
