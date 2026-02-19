using System.Threading.Tasks;

class WordCounterProgram
{
    static async Task Main(string[] args)
    {
        var options = WordCounterOptions.FromEnvironment();
        var service = new WordCounterService(options);
        var formatter = new ConsoleReportFormatter();
        var runner = new CliRunner(service, formatter);

        int exitCode = await runner.RunAsync(args);
        System.Environment.ExitCode = exitCode;
    }
}
