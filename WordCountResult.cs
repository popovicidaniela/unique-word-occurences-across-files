using System.Collections.Generic;
using System.Linq;

public sealed class WordCountResult
{
    private readonly long _totalWordOccurrences;

    public IReadOnlyDictionary<string, long> Counts { get; }
    public IReadOnlyList<string> ProcessingErrors { get; }

    public int UniqueWordCount => Counts.Count;
    public long TotalWordOccurrences => _totalWordOccurrences;

    public WordCountResult(IReadOnlyDictionary<string, long> counts, IReadOnlyList<string> processingErrors)
    {
        Counts = counts;
        ProcessingErrors = processingErrors;
        _totalWordOccurrences = counts.Values.Sum();
    }
}
