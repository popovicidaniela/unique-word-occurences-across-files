using System;
using System.Text;

public sealed class StreamingWordTokenizer : IWordTokenizer
{
    private readonly StringBuilder _currentWord;

    public StreamingWordTokenizer(int initialWordCapacity = WordCounterOptions.InitialWordCapacity)
    {
        _currentWord = new StringBuilder(initialWordCapacity);
    }

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
