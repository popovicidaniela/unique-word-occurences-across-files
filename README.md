# unique-word-occurences-across-files

Given multiple text files, a C# program counts the occurrences of each unique word in the files and aggregates the results. The implementation is designed to minimize running time and memory usage, independent of the number of files or file size.

## Overview

This project provides an optimized command-line application that efficiently processes large text files to generate word frequency statistics. The implementation uses modern C# features and best practices for performance and scalability.

## Key Features

- **Parallel File Processing**: Processes multiple files concurrently using all available CPU cores
- **Memory-Efficient Streaming**: Reads files line-by-line with 64KB buffering to handle large files without loading entire contents into memory
- **Thread-Safe Aggregation**: Uses `ConcurrentDictionary` for safe concurrent word count updates
- **Case-Insensitive Analysis**: Normalizes words to lowercase for accurate frequency counting
- **Robust Word Extraction**: Uses compiled regex patterns to extract words, ignoring punctuation
- **Comprehensive Statistics**: Displays total unique words, total occurrences, and top 50 words by frequency

## Performance Optimizations

1. **Streaming I/O**: Files are processed line-by-line rather than loading entire contents into memory
2. **Buffered Reading**: Uses 64KB buffer size for optimal I/O performance with large files
3. **Parallel Processing**: Files are processed concurrently with `Parallel.ForEachAsync` controlling parallelism based on the number of processors available, or number of files
4. **Compiled Regex**: Word extraction regex is compiled once for reuse across lines
5. **Concurrent Dictionary**: Thread-safe word count aggregation without explicit locking
6. **Lazy Evaluation**: Results are sorted only once after all processing is complete

## Building the Project

### Requirements
- .NET 10.0 SDK or later

### Build Commands

```bash
# Build the project
dotnet build WordCounter.csproj

# Publish as self-contained executable
dotnet publish -c Release WordCounter.csproj
```

## Usage

### Basic Usage

```bash
# Process single file
dotnet run --project WordCounter.csproj -- sample1.txt

# Process multiple files
dotnet run --project WordCounter.csproj -- sample1.txt sample2.txt sample3.txt

# Process with globbing (shell expansion)
dotnet run --project WordCounter.csproj -- *.txt
```

### Running the Executable

```bash
# After publishing
./bin/Release/net10.0/WordCounter file1.txt file2.txt file3.txt
```

## Example Output

```
Processing 3 file(s)...

Total unique words: 42
Total word occurrences: 287

Word Counts (Top 50 by frequency):
--------------------------------------------------
Word                           Count
--------------------------------------------------
the                               25
quick                             18
fox                               15
dog                               14
brown                             12
and                               11
is                                 9
was                                8
very                               7
...
```

## Algorithm Description

### Word Counting Algorithm

1. **File Reading**: Each file is processed asynchronously with line-by-line streaming
   - Uses `StreamReader` with UTF-8 encoding and 64KB buffer
   - Skips empty/whitespace-only lines for efficiency

2. **Word Extraction**: Line content is parsed using regex pattern `\b\w+\b`
   - `\b` = word boundary
   - `\w+` = one or more word characters (letters, digits, underscore)
   - Case normalization with `ToLowerInvariant()`

3. **Aggregation**: Word counts are updated in concurrent dictionary
   - `AddOrUpdate()` atomically increments counts
   - No lock contention due to concurrent dictionary implementation

4. **Results**: Final results are sorted by frequency (descending) then alphabetically

### Time Complexity

- **Per File**: O(n) where n = total characters in file
- **Overall**: O(n) for all files (with parallelization speedup)
- **Sorting Results**: O(k log k) where k = unique word count (typically much smaller than n)

### Space Complexity

- **File Buffer**: O(1) - fixed 64KB buffer regardless of file size
- **Word Storage**: O(k) where k = number of unique words
- **Total**: O(k) without loading entire files into memory

## File Structure

```
/
├── WordCounter.sln         # Solution file
├── WordCounter.csproj      # Project file
├── Program.cs              # Main implementation
├── README.md               # This file
├── sample1.txt             # Sample test file
├── sample2.txt             # Sample test file
└── sample3.txt             # Sample test file
```

## Testing

Run the program with the provided sample files:

```bash
dotnet run -- sample1.txt sample2.txt sample3.txt
```

## Scalability Considerations

**Handles Large Files**: Streaming I/O keeps memory usage constant regardless of file size
**Handles Many Files**: Parallel processing divides work across CPU cores
**Handles Long Lines**: Line-based processing doesn't fail on files with very long lines
**Graceful Degradation**: Error handling allows processing to continue even if some files fail

## Error Handling

- Invalid file paths are skipped with a warning
- File I/O errors are caught and reported without stopping entire process
- Encoding errors are handled gracefully (UTF-8 with fallback support)

## Dependencies

- .NET 10.0 or later
- No external NuGet packages required
- Uses only built-in .NET libraries:
  - `System.IO` - File streaming and buffered reading
  - `System.Collections.Concurrent` - Thread-safe ConcurrentDictionary
  - `System.Text.RegularExpressions` - Word extraction pattern matching
  - `System.Threading.Tasks` - Asynchronous and parallel processing

## License

MIT
