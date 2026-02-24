using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public sealed class CliRunner
{
    private readonly IWordCounterService _wordCounterService;
    private readonly IWordCountReportFormatter _reportFormatter;

    public CliRunner(IWordCounterService wordCounterService, IWordCountReportFormatter reportFormatter)
    {
        _wordCounterService = wordCounterService;
        _reportFormatter = reportFormatter;
    }

    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: WordCounter <file_path> [file_path2] [file_path3] ...");
            Console.WriteLine("Example: WordCounter file1.txt file2.txt file3.txt");
            return 1;
        }

        var filePaths = args.Where(File.Exists).ToList();

        if (filePaths.Count == 0)
        {
            Console.WriteLine("Error: No valid files found.");
            return 1;
        }

        if (filePaths.Count < args.Length)
        {
            Console.WriteLine($"Warning: {args.Length - filePaths.Count} file(s) not found and will be skipped.");
        }

        Console.WriteLine($"Processing {filePaths.Count} file(s)...\n");

        WordCountResult result = await _wordCounterService.CountWordsAsync(filePaths, cancellationToken);
        _reportFormatter.Print(result, Console.Out);

        if (result.ProcessingErrors.Count > 0)
        {
            return 2;
        }

        return 0;
    }
}
