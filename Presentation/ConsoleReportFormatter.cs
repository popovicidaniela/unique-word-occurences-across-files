using System.IO;
using System.Linq;

public sealed class ConsoleReportFormatter : IWordCountReportFormatter
{
    public void Print(WordCountResult result, TextWriter writer)
    {
        foreach (string processingError in result.ProcessingErrors)
        {
            writer.WriteLine(processingError);
        }

        if (result.UniqueWordCount == 0)
        {
            writer.WriteLine("No words found in the provided files.");
            return;
        }

        var sortedResults = result.Counts
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key)
            .ToList();

        writer.WriteLine($"Total unique words: {result.UniqueWordCount}");
        writer.WriteLine($"Total word occurrences: {result.TotalWordOccurrences}\n");
        writer.WriteLine("Word Counts (Top 50 by frequency):");
        writer.WriteLine(new string('-', 50));
        writer.WriteLine($"{"Word",-30} {"Count",15}");
        writer.WriteLine(new string('-', 50));

        foreach (var kvp in sortedResults.Take(50))
        {
            writer.WriteLine($"{kvp.Key,-30} {kvp.Value,15:N0}");
        }

        if (sortedResults.Count > 50)
        {
            writer.WriteLine($"\n... and {sortedResults.Count - 50} more unique words");
        }
    }
}
