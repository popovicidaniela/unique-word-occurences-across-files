using System;

public interface IWordTokenizer
{
    void ProcessChunk(ReadOnlySpan<char> chunk, Action<string> onWord);
    void Complete(Action<string> onWord);
}
