using System.Collections.Generic;
using System.Linq;

public sealed class WordCountResult
{
    public IReadOnlyDictionary<string, long> Counts { get; }
    public IReadOnlyList<string> ProcessingErrors { get; }

    public int UniqueWordCount => Counts.Count;
    public long TotalWordOccurrences => Counts.Values.Sum();

    public WordCountResult(IReadOnlyDictionary<string, long> counts, IReadOnlyList<string> processingErrors)
    {
        Counts = counts;
        ProcessingErrors = processingErrors;
    }
}
