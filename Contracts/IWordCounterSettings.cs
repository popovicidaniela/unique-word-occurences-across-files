public interface IWordCounterSettings
{
    int ChunkSize { get; }
    int ResolveParallelism(int fileCount);
}