using System.IO;

public interface IWordCountReportFormatter
{
    void Print(WordCountResult result, TextWriter writer);
}
