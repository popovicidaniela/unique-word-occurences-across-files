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
    // _tokenizerFactory stores a function that creates new instances of IWordTokenizer. 
    // This allows the service to create a separate tokenizer for each file, ensuring thread safety and 
    // proper state management during concurrent processing.
    private readonly Func<IWordTokenizer> _tokenizerFactory;

    public WordCounterService(WordCounterOptions options, Func<IWordTokenizer> tokenizerFactory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
         _tokenizerFactory = tokenizerFactory ?? throw new ArgumentNullException(nameof(tokenizerFactory));
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
        IWordTokenizer tokenizer = _tokenizerFactory() ?? throw new InvalidOperationException("Tokenizer factory returned null.");

        int charsRead;
        // read into buffer, if count is 0, stop, otherwise process and read next chunk
        // AsMemory passes the writable memory region of that array.
        while ((charsRead = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            // AsSpan passes a window over the existing char[] without copying, so tokenizer reads only valid chars and avoids per-chunk allocations
            // for each word passed in onWord callback, counts are updated atomically using AddOrUpdate, 
            // ensuring thread safety when multiple threads update the same word count concurrently.
            tokenizer.ProcessChunk(buffer.AsSpan(0, charsRead), word =>
            {
                // performs increment atomically
                // first word hit starts at 1, later hits increment from current value, even when many threads hit same word simultaneously.
                counts.AddOrUpdate(word, 1, (_, oldValue) => oldValue + 1);
            });
        }

        tokenizer.Complete(word =>
        {
            counts.AddOrUpdate(word, 1, (_, oldValue) => oldValue + 1);
        });
    }
}
