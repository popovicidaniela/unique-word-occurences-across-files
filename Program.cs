using System;
using System.Threading.Tasks;

class WordCounterProgram
{
    static async Task Main(string[] args)
    {
        var options = WordCounterOptions.FromEnvironment();
        Func<IWordTokenizer> tokenizerFactory = () => new StreamingWordTokenizer();
        var service = new WordCounterService(options, tokenizerFactory);
        var formatter = new ConsoleReportFormatter();
        var runner = new CliRunner(service, formatter);

        int exitCode = await runner.RunAsync(args);
        System.Environment.ExitCode = exitCode;
    }
}
