using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
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

    [Fact]
    public async Task CreatesTokenizerFromFactoryPerFile()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string file1 = Path.Combine(tempDirectory, "file1.txt");
            string file2 = Path.Combine(tempDirectory, "file2.txt");

            await File.WriteAllTextAsync(file1, "alpha beta");
            await File.WriteAllTextAsync(file2, "gamma delta");

            int factoryInvocationCount = 0;
            Func<IWordTokenizer> tokenizerFactory = () =>
            {
                Interlocked.Increment(ref factoryInvocationCount);
                return new StreamingWordTokenizer();
            };

            var options = new WordCounterOptions(chunkSize: 8, maxParallelism: 2);
            var service = new WordCounterService(options, tokenizerFactory);

            WordCountResult result = await service.CountWordsAsync(new[] { file1, file2 });

            Assert.Empty(result.ProcessingErrors);
            Assert.Equal(2, factoryInvocationCount);
        }
        finally
        {
            DeleteDirectoryQuietly(tempDirectory);
        }
    }

    [Fact]
    public async Task UsesDistinctTokenizerInstancePerProcessedFile()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string file1 = Path.Combine(tempDirectory, "first.txt");
            string file2 = Path.Combine(tempDirectory, "second.txt");
            string file3 = Path.Combine(tempDirectory, "third.txt");

            await File.WriteAllTextAsync(file1, "alpha");
            await File.WriteAllTextAsync(file2, "beta");
            await File.WriteAllTextAsync(file3, "gamma");

            var createdInstances = new ConcurrentBag<IWordTokenizer>();
            Func<IWordTokenizer> tokenizerFactory = () =>
            {
                IWordTokenizer tokenizer = new StreamingWordTokenizer();
                createdInstances.Add(tokenizer);
                return tokenizer;
            };

            var options = new WordCounterOptions(chunkSize: 8, maxParallelism: 3);
            var service = new WordCounterService(options, tokenizerFactory);

            WordCountResult result = await service.CountWordsAsync(new[] { file1, file2, file3 });

            Assert.Empty(result.ProcessingErrors);
            Assert.Equal(3, createdInstances.Count);
            Assert.Equal(3, createdInstances.Distinct().Count());
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
