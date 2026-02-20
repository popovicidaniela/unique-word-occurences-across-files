using System;

public sealed class WordCounterOptions
{
    public const int DefaultChunkSize = 64 * 1024;
    public const int InitialWordCapacity = 64;

    private const string ChunkSizeEnvVar = "WORD_COUNTER_CHUNK_SIZE";
    private const string MaxParallelismEnvVar = "WORD_COUNTER_MAX_PARALLELISM";

    public int ChunkSize { get; }
    public int? MaxParallelism { get; }

    public WordCounterOptions(int chunkSize, int? maxParallelism)
    {
        ChunkSize = chunkSize > 0 ? chunkSize : DefaultChunkSize;
        MaxParallelism = maxParallelism is > 0 ? maxParallelism : null;
    }

    public static WordCounterOptions FromEnvironment()
    {
        int chunkSize = ParsePositiveInt(ChunkSizeEnvVar) ?? DefaultChunkSize;
        int? maxParallelism = ParsePositiveInt(MaxParallelismEnvVar);

        return new WordCounterOptions(chunkSize, maxParallelism);
    }

    public int ResolveParallelism(int fileCount)
    {
        if (fileCount <= 0)
        {
            return 1;
        }

        if (MaxParallelism.HasValue)
        {
            return Math.Min(fileCount, MaxParallelism.Value);
        }

        int defaultParallelism = Math.Max(1, Environment.ProcessorCount * 2);
        return Math.Min(fileCount, defaultParallelism);
    }

    private static int? ParsePositiveInt(string variableName)
    {
        string? value = Environment.GetEnvironmentVariable(variableName);
        if (!string.IsNullOrWhiteSpace(value) && int.TryParse(value, out int parsed) && parsed > 0)
        {
            return parsed;
        }

        return null;
    }
}
