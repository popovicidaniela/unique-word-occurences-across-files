using System.Collections.Generic;
using Xunit;

namespace WordCounter.Tests;

public class StreamingWordTokenizerTests
{
    [Fact]
    public void PreservesWordAcrossChunkBoundaries()
    {
        IWordTokenizer tokenizer = new StreamingWordTokenizer();
        var words = new List<string>();

        tokenizer.ProcessChunk("inter".AsSpan(), words.Add);
        tokenizer.ProcessChunk("national test".AsSpan(), words.Add);
        tokenizer.Complete(words.Add);

        Assert.Equal(2, words.Count);
        Assert.Equal("international", words[0]);
        Assert.Equal("test", words[1]);
    }

    [Fact]
    public void SplitsWordsByNonWordCharacters()
    {
        IWordTokenizer tokenizer = new StreamingWordTokenizer();
        var words = new List<string>();

        tokenizer.ProcessChunk("Hello,world!foo_bar 123".AsSpan(), words.Add);
        tokenizer.Complete(words.Add);

        Assert.Equal(new[] { "hello", "world", "foo_bar", "123" }, words);
    }
}
