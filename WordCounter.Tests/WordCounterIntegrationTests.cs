using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace WordCounter.Tests;

public class WordCounterIntegrationTests
{
    [Fact]
    public async Task CountsWordsInSingleFile()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string filePath = Path.Combine(tempDirectory, "single.txt");
            await File.WriteAllTextAsync(filePath, "Hello hello hello_world 123");

            ProcessResult result = await RunWordCounterAsync(filePath);

            Assert.Equal(0, result.ExitCode);
            Assert.Contains("Total unique words: 3", result.Output);
            Assert.Contains("Total word occurrences: 4", result.Output);
        }
        finally
        {
            DeleteDirectoryQuietly(tempDirectory);
        }
    }

    [Fact]
    public async Task AggregatesAcrossMultipleFiles()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string file1 = Path.Combine(tempDirectory, "a.txt");
            string file2 = Path.Combine(tempDirectory, "b.txt");

            await File.WriteAllTextAsync(file1, "alpha beta alpha");
            await File.WriteAllTextAsync(file2, "beta gamma");

            ProcessResult result = await RunWordCounterAsync(file1, file2);

            Assert.Equal(0, result.ExitCode);
            Assert.Contains("Total unique words: 3", result.Output);
            Assert.Contains("Total word occurrences: 5", result.Output);
            Assert.Contains("alpha", result.Output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("beta", result.Output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("gamma", result.Output, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDirectoryQuietly(tempDirectory);
        }
    }

    [Fact]
    public async Task HandlesVeryLongSingleLineWithoutBreakingWord()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string filePath = Path.Combine(tempDirectory, "longline.txt");
            string longWord = new string('a', 70_000);
            await File.WriteAllTextAsync(filePath, longWord + " " + longWord);

            ProcessResult result = await RunWordCounterAsync(filePath);

            Assert.Equal(0, result.ExitCode);
            Assert.Contains("Total unique words: 1", result.Output);
            Assert.Contains("Total word occurrences: 2", result.Output);
        }
        finally
        {
            DeleteDirectoryQuietly(tempDirectory);
        }
    }

    private static async Task<ProcessResult> RunWordCounterAsync(params string[] filePaths)
    {
        string repositoryRoot = GetRepositoryRoot();

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = repositoryRoot
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add("WordCounter.csproj");
        startInfo.ArgumentList.Add("--");

        foreach (string filePath in filePaths)
        {
            startInfo.ArgumentList.Add(filePath);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        string output = await standardOutputTask;
        string error = await standardErrorTask;

        return new ProcessResult(process.ExitCode, output + error);
    }

    private static string GetRepositoryRoot()
    {
        string? currentDirectory = AppContext.BaseDirectory;

        while (!string.IsNullOrEmpty(currentDirectory))
        {
            if (File.Exists(Path.Combine(currentDirectory, "WordCounter.csproj")))
            {
                return currentDirectory;
            }

            string? parent = Directory.GetParent(currentDirectory)?.FullName;
            if (string.Equals(parent, currentDirectory, StringComparison.Ordinal))
            {
                break;
            }

            currentDirectory = parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing WordCounter.csproj");
    }

    private static string CreateTempDirectory()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "WordCounterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        return tempDirectory;
    }

    private static void DeleteDirectoryQuietly(string directoryPath)
    {
        try
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
        }
        catch
        {
            // Ignore cleanup failures in tests.
        }
    }

    private sealed record ProcessResult(int ExitCode, string Output);
}
