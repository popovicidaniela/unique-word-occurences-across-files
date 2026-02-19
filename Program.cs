using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class WordCounterProgram
{
    private const int DefaultChunkSize = 64 * 1024;
    private const int InitialWordCapacity = 64;
    private const string ChunkSizeEnvVar = "WORD_COUNTER_CHUNK_SIZE";
    private const string MaxParallelismEnvVar = "WORD_COUNTER_MAX_PARALLELISM";
    private static readonly ConcurrentDictionary<string, long> WordCounts = 
        new ConcurrentDictionary<string, long>();

    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: WordCounter <file_path> [file_path2] [file_path3] ...");
            Console.WriteLine("Example: WordCounter file1.txt file2.txt file3.txt");
            return;
        }

        // Get valid file paths
        var filePaths = args
            .Where(File.Exists)
            .ToList();

        if (filePaths.Count == 0)
        {
            Console.WriteLine("Error: No valid files found.");
            return;
        }

        if (filePaths.Count < args.Length)
        {
            Console.WriteLine($"Warning: {args.Length - filePaths.Count} file(s) not found and will be skipped.");
        }

        Console.WriteLine($"Processing {filePaths.Count} file(s)...\n");

        // Process files in parallel with buffered reading for optimal performance
        await ProcessFilesAsync(filePaths);

        // Display results sorted by count (descending) then alphabetically
        DisplayResults();
    }

    private static async Task ProcessFilesAsync(List<string> filePaths)
    {
        // Use parallel processing with configurable degree of parallelism
        int maxParallelism = GetMaxDegreeOfParallelism(filePaths.Count);
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxParallelism
        };

        await Parallel.ForEachAsync(filePaths, parallelOptions, async (filePath, ct) =>
        {
            try
            {
                await ProcessFileAsync(filePath, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
            }
        });
    }

    private static int GetMaxDegreeOfParallelism(int fileCount)
    {
        if (fileCount <= 0)
        {
            return 1;
        }

        string? configuredValue = Environment.GetEnvironmentVariable(MaxParallelismEnvVar);
        if (!string.IsNullOrWhiteSpace(configuredValue) && int.TryParse(configuredValue, out int parsedValue) && parsedValue > 0)
        {
            return Math.Min(fileCount, parsedValue);
        }

        int defaultParallelism = Math.Max(1, Environment.ProcessorCount * 2);
        return Math.Min(fileCount, defaultParallelism);
    }

    private static int GetChunkSize()
    {
        string? configuredValue = Environment.GetEnvironmentVariable(ChunkSizeEnvVar);
        if (!string.IsNullOrWhiteSpace(configuredValue) && int.TryParse(configuredValue, out int parsedValue) && parsedValue > 0)
        {
            return parsedValue;
        }

        return DefaultChunkSize;
    }

    private static async Task ProcessFileAsync(string filePath, System.Threading.CancellationToken ct)
    {
        try
        {
            int chunkSize = GetChunkSize();

            // Use chunk-based tokenization to handle arbitrarily long lines safely
            using (var reader = new StreamReader(filePath, Encoding.UTF8, true, bufferSize: chunkSize))
            {
                char[] buffer = new char[chunkSize];
                var currentWord = new StringBuilder(InitialWordCapacity);
                int charsRead;

                while ((charsRead = await reader.ReadAsync(buffer.AsMemory(0, chunkSize), ct)) > 0)
                {
                    ProcessChunk(buffer, charsRead, currentWord);
                }

                FlushCurrentWord(currentWord);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to read file {filePath}", ex);
        }
    }

    private static void ProcessChunk(char[] buffer, int charsRead, StringBuilder currentWord)
    {
        for (int index = 0; index < charsRead; index++)
        {
            char character = buffer[index];
            if (IsWordCharacter(character))
            {
                currentWord.Append(char.ToLowerInvariant(character));
            }
            else
            {
                FlushCurrentWord(currentWord);
            }
        }
    }

    private static bool IsWordCharacter(char character)
    {
        return char.IsLetterOrDigit(character) || character == '_';
    }

    private static void FlushCurrentWord(StringBuilder currentWord)
    {
        if (currentWord.Length == 0)
        {
            return;
        }

        string word = currentWord.ToString();
        currentWord.Clear();

        WordCounts.AddOrUpdate(word, 1, (_, oldValue) => oldValue + 1);
    }

    private static void DisplayResults()
    {
        if (WordCounts.IsEmpty)
        {
            Console.WriteLine("No words found in the provided files.");
            return;
        }

        var sortedResults = WordCounts
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key)
            .ToList();

        Console.WriteLine($"Total unique words: {WordCounts.Count}");
        Console.WriteLine($"Total word occurrences: {WordCounts.Values.Sum()}\n");
        Console.WriteLine("Word Counts (Top 50 by frequency):");
        Console.WriteLine(new string('-', 50));
        Console.WriteLine($"{"Word",-30} {"Count",15}");
        Console.WriteLine(new string('-', 50));

        foreach (var kvp in sortedResults.Take(50))
        {
            Console.WriteLine($"{kvp.Key,-30} {kvp.Value,15:N0}");
        }

        if (sortedResults.Count > 50)
        {
            Console.WriteLine($"\n... and {sortedResults.Count - 50} more unique words");
        }
    }
}
