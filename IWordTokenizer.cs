using System;

/// <summary>
/// Tokenizes incoming character data into words using a stateful streaming model.
/// </summary>
/// <remarks>
/// Implementations are stateful and are intended for per-operation usage only.
/// They are not thread-safe and must not be shared across concurrent operations.
/// Create a new tokenizer instance for each independently processed stream/file.
/// </remarks>
public interface IWordTokenizer
{
    /// <summary>
    /// Processes the next chunk of characters and emits completed words via <paramref name="onWord"/>.
    /// </summary>
    /// <param name="chunk">The next contiguous chunk of characters from the input stream.</param>
    /// <param name="onWord">Callback invoked for each completed word.</param>
    void ProcessChunk(ReadOnlySpan<char> chunk, Action<string> onWord);

    /// <summary>
    /// Completes tokenization and emits any final pending word via <paramref name="onWord"/>.
    /// </summary>
    /// <param name="onWord">Callback invoked for each completed word.</param>
    void Complete(Action<string> onWord);
}
