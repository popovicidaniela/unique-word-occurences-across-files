using System;
using System.Text;

/// <summary>
/// Stateful streaming tokenizer that emits lowercased words composed of letters, digits, and underscore.
/// </summary>
/// <remarks>
/// This type is not thread-safe. Use one instance per operation/file and do not share
/// the same instance across concurrent processing tasks.
/// </remarks>
public sealed class StreamingWordTokenizer : IWordTokenizer
{
    private readonly StringBuilder _currentWord;

    /// <summary>
    /// Initializes a new tokenizer instance with an optional initial word buffer capacity.
    /// </summary>
    /// <param name="initialWordCapacity">Initial capacity for the internal word buffer.</param>
    public StreamingWordTokenizer(int initialWordCapacity = WordCounterOptions.InitialWordCapacity)
    {
        _currentWord = new StringBuilder(initialWordCapacity);
    }

    /// <inheritdoc />
    public void ProcessChunk(ReadOnlySpan<char> chunk, Action<string> onWord)
    {
        for (int index = 0; index < chunk.Length; index++)
        {
            char character = chunk[index];
            if (IsWordCharacter(character))
            {
                _currentWord.Append(char.ToLowerInvariant(character));
            }
            else
            {
                Flush(onWord);
            }
        }
    }

    /// <inheritdoc />
    public void Complete(Action<string> onWord)
    {
        Flush(onWord);
    }

    private static bool IsWordCharacter(char character)
    {
        return char.IsLetterOrDigit(character) || character == '_';
    }

    private void Flush(Action<string> onWord)
    {
        if (_currentWord.Length == 0)
        {
            return;
        }

        string word = _currentWord.ToString();
        _currentWord.Clear();
        onWord(word);
    }
}
