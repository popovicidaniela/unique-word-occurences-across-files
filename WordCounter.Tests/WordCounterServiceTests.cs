using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace WordCounter.Tests;

public class WordCounterServiceTests
{
    [Fact]
    public async Task CountsWordsUsingServiceDirectly()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string filePath = Path.Combine(tempDirectory, "service.txt");
            await File.WriteAllTextAsync(filePath, "alpha beta alpha");

            var options = new WordCounterOptions(chunkSize: 8, maxParallelism: 1);
            var service = new WordCounterService(options);

            WordCountResult result = await service.CountWordsAsync(new[] { filePath });

            Assert.Equal(2, result.UniqueWordCount);
            Assert.Equal(3, result.TotalWordOccurrences);
            Assert.Equal(2, result.Counts["alpha"]);
            Assert.Equal(1, result.Counts["beta"]);
            Assert.Empty(result.ProcessingErrors);
        }
        finally
        {
            DeleteDirectoryQuietly(tempDirectory);
        }
    }

    private static string CreateTempDirectory()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "WordCounterServiceTests", Guid.NewGuid().ToString("N"));
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
}
