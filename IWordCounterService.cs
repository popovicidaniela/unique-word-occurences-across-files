using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IWordCounterService
{
    Task<WordCountResult> CountWordsAsync(IReadOnlyList<string> filePaths, CancellationToken cancellationToken = default);
}
