using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public sealed class WordCounterService : IWordCounterService
{
    private readonly WordCounterOptions _options;

    public WordCounterService(WordCounterOptions options)
    {
        _options = options;
    }

    public async Task<WordCountResult> CountWordsAsync(IReadOnlyList<string> filePaths, CancellationToken cancellationToken = default)
    {
        var counts = new ConcurrentDictionary<string, long>(StringComparer.Ordinal);
        var errors = new ConcurrentBag<string>();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _options.ResolveParallelism(filePaths.Count),
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(filePaths, parallelOptions, async (filePath, ct) =>
        {
            try
            {
                await ProcessFileAsync(filePath, counts, ct);
            }
            catch (Exception ex)
            {
                errors.Add($"Error processing file {filePath}: {ex.Message}");
            }
        });

        var snapshot = new Dictionary<string, long>(counts, StringComparer.Ordinal);
        return new WordCountResult(snapshot, errors.ToList());
    }

    private async Task ProcessFileAsync(string filePath, ConcurrentDictionary<string, long> counts, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(filePath, Encoding.UTF8, true, bufferSize: _options.ChunkSize);

        char[] buffer = new char[_options.ChunkSize];
        IWordTokenizer tokenizer = new StreamingWordTokenizer();

        int charsRead;
        while ((charsRead = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            tokenizer.ProcessChunk(buffer.AsSpan(0, charsRead), word =>
            {
                counts.AddOrUpdate(word, 1, (_, oldValue) => oldValue + 1);
            });
        }

        tokenizer.Complete(word =>
        {
            counts.AddOrUpdate(word, 1, (_, oldValue) => oldValue + 1);
        });
    }
}
