using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

class WordCounterProgram
{
    static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddSingleton(WordCounterOptions.FromEnvironment());
        builder.Services.AddSingleton<IWordCounterSettings>(serviceProvider =>
            serviceProvider.GetRequiredService<WordCounterOptions>());

        builder.Services.AddTransient<IWordTokenizer, StreamingWordTokenizer>();
        builder.Services.AddSingleton<Func<IWordTokenizer>>(serviceProvider =>
            () => serviceProvider.GetRequiredService<IWordTokenizer>());

        builder.Services.AddSingleton<IWordCounterService, WordCounterService>();
        builder.Services.AddSingleton<IWordCountReportFormatter, ConsoleReportFormatter>();
        builder.Services.AddSingleton<CliRunner>();

        using IHost host = builder.Build();
        CliRunner runner = host.Services.GetRequiredService<CliRunner>();

        int exitCode = await runner.RunAsync(args);
        System.Environment.ExitCode = exitCode;
    }
}
