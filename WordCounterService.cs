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
    private readonly IWordCounterSettings _settings;
    private readonly Func<IWordTokenizer> _tokenizerFactory;

    public WordCounterService(IWordCounterSettings settings, Func<IWordTokenizer> tokenizerFactory)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _tokenizerFactory = tokenizerFactory ?? throw new ArgumentNullException(nameof(tokenizerFactory));
    }

    public async Task<WordCountResult> CountWordsAsync(IReadOnlyList<string> filePaths, CancellationToken cancellationToken = default)
    {
        var counts = new ConcurrentDictionary<string, long>(StringComparer.Ordinal);
        var errors = new ConcurrentBag<string>();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _settings.ResolveParallelism(filePaths.Count),
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
        using var reader = new StreamReader(filePath, Encoding.UTF8, true, bufferSize: _settings.ChunkSize);

        char[] buffer = new char[_settings.ChunkSize];
        IWordTokenizer tokenizer = _tokenizerFactory() ?? throw new InvalidOperationException("Tokenizer factory returned null.");

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
