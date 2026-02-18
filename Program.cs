using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class WordCounterProgram
{
    private static readonly Regex WordPattern = new Regex(@"\b\w+\b", RegexOptions.Compiled);
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
        // Use parallel processing with a degree of parallelism based on processor count
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Min(filePaths.Count, Environment.ProcessorCount)
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

    private static async Task ProcessFileAsync(string filePath, System.Threading.CancellationToken ct)
    {
        try
        {
            // Use StreamReader with a reasonable buffer size (64KB) for efficient I/O
            using (var reader = new StreamReader(filePath, System.Text.Encoding.UTF8, true, bufferSize: 65536))
            {
                string? line;
                while ((line = await reader.ReadLineAsync(ct)) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Extract words from the line
                    ProcessLine(line);
                }
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

    private static void ProcessLine(string line)
    {
        // Use regex to extract words (alphanumeric sequences)
        var matches = WordPattern.Matches(line);

        foreach (Match match in matches)
        {
            string word = match.Value.ToLowerInvariant();

            // Increment count for this word
            WordCounts.AddOrUpdate(word, 1, (key, oldValue) => oldValue + 1);
        }
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
