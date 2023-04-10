using System.Text.RegularExpressions;
using System.Text;

namespace Otterkit;

public static partial class Preprocessor
{
    internal static bool HasDetectedSourceFormat;
    internal static DirectiveType LastDirective = DirectiveType.None;
    internal static string Workspace => Directory.GetCurrentDirectory(); 

    public static List<Token> Preprocess(string entryPoint)
    {
        if (!File.Exists(entryPoint))
        {
            Error
            .Build(ErrorType.Compilation, ConsoleColor.Red, 950, """
                Entry point file not found.
                """)
            .WithStartingError($"""
                Unable to find the entry point file specified: {entryPoint}
                """)
            .CloseError();

            Environment.Exit(1);
        }

        var relativeEntryPoint = Path.GetRelativePath(Workspace, entryPoint);

        CompilerOptions.EntryPoint = relativeEntryPoint;

        var allSourceFiles = Directory.EnumerateFiles(Workspace, "*.cob", SearchOption.AllDirectories)
            .Select(static path => Path.GetRelativePath(Workspace, path));
        
        CompilerContext.FileNames.Add(relativeEntryPoint);

        var tokens = ReadSourceFile(relativeEntryPoint).Result;

        foreach (var file in allSourceFiles)
        {
            if (file.Equals(relativeEntryPoint)) continue;

            CompilerContext.FileNames.Add(file);

            tokens = ReadSourceFile(file).Result;
        }

        PreprocessCopybooks(CompilerContext.SourceTokens);

        return CompilerContext.SourceTokens;
    }

    public static void PreprocessSourceFormat(ReadOnlySpan<byte> bytes, Span<char> chars)
    {
        var charCount = Encoding.UTF8.GetCharCount(bytes);
        var maxStackLimit = 256;

        Span<char> sourceChars = charCount <= maxStackLimit 
            ? stackalloc char[charCount]
            : new char[charCount];

        Encoding.UTF8.GetChars(bytes, sourceChars);

        if (!HasDetectedSourceFormat && CompilerOptions.SourceFormat == SourceFormat.Auto)
        {
            if (sourceChars.Length >= 9 && sourceChars[7] is '>' && sourceChars[8] is '>')
            {
                CompilerOptions.SourceFormat = SourceFormat.Fixed;
                HasDetectedSourceFormat = true;
            }

            if (sourceChars.Length >= 7 && sourceChars[6] is '*' or '-' or '/' or ' ')
            {
                CompilerOptions.SourceFormat = SourceFormat.Fixed;
                HasDetectedSourceFormat = true;
            }
            else
            {
                CompilerOptions.SourceFormat = SourceFormat.Free;
                HasDetectedSourceFormat = true;
            }

            if (sourceChars.Slice(0, 7).Trim().StartsWith("*>"))
            {
                CompilerOptions.SourceFormat = SourceFormat.Free;
                HasDetectedSourceFormat = true;
            }

            if (sourceChars.Slice(0, 7).Trim().StartsWith(">>"))
            {
                CompilerOptions.SourceFormat = SourceFormat.Free;
                HasDetectedSourceFormat = true;
            }

            if (sourceChars.Trim().Length == 0)
            {
                CompilerOptions.SourceFormat = SourceFormat.Auto;
                HasDetectedSourceFormat = false;
            }
        }

        if (CompilerOptions.SourceFormat == SourceFormat.Fixed || !HasDetectedSourceFormat)
        {
            if (sourceChars.Length >= CompilerOptions.ColumnLength)
            {
                // Removes everything after the max column length
                sourceChars.Slice(CompilerOptions.ColumnLength).Fill(' ');
            }

            // Removes the sequence number area
            if (sourceChars.Length >= 7)
            {
                sourceChars.Slice(0, 6).Fill(' ');
            }
            else
            {
                sourceChars.Fill(' ');
            }

            if (sourceChars.Length >= 7 && sourceChars[6].Equals('*'))
            {
                // Removes all fixed format comment lines
                sourceChars.Fill(' ');
            }

            int commentIndex = sourceChars.IndexOf("*>");
            if (commentIndex > -1)
            {
                // Removes all floating comments
                sourceChars = sourceChars.Slice(0, commentIndex);
            }

            if (sourceChars.Length >= 1)
            {
                sourceChars[0] = ' ';
            }
        }

        if (CompilerOptions.SourceFormat == SourceFormat.Free)
        {
            int commentIndex = sourceChars.IndexOf("*>");
            if (commentIndex > -1)
            {
                // Removes all floating comments
                sourceChars = sourceChars.Slice(0, commentIndex);
            }
        }

        sourceChars.CopyTo(chars);
    }

    public static void PreprocessDirective(ReadOnlySpan<char> directiveChars, int lineNumber)
    {
        List<DirectiveToken> directiveTokens = new();
        var index = 0;

        foreach (var token in PreprocessorRegex().EnumerateMatches(directiveChars))
        {
            ReadOnlySpan<char> currentMatch = directiveChars.Slice(token.Index, token.Length);

            DirectiveToken tokenized = new(new string(currentMatch), lineNumber);
            directiveTokens.Add(tokenized);
        }
        
        if (CurrentEquals(">>SOURCE"))
        {
            Continue();

            if (CurrentEquals("FORMAT")) Continue();

            if (CurrentEquals("IS")) Continue();

            if (CurrentEquals("FREE"))
            {
                CompilerOptions.SourceFormat = SourceFormat.Free;
            }

            if (CurrentEquals("FIXED"))
            {
                CompilerOptions.SourceFormat = SourceFormat.Fixed;
            }
        }

        DirectiveToken Current()
        {
            return directiveTokens[index];
        }

        bool CurrentEquals(string stringToCompare)
        {
            return Current().value.Equals(stringToCompare, StringComparison.OrdinalIgnoreCase);
        }

        void Continue()
        {
            if (index >= directiveTokens.Count - 1) return;

            index += 1;
        }
    }

    public static void PreprocessCopybooks(List<Token> sourceTokens)
    {
        var tokenIndex = 0;

        while (!(tokenIndex >= sourceTokens.Count - 1))
        {
            if (!CurrentEquals("COPY"))
            {
                Continue();
                continue;
            }

            if (CurrentEquals("COPY"))
            {
                var statementIndex = tokenIndex;

                Continue();

                var copybookName = Current().Value;

                CompilerContext.FileNames.Add(copybookName);

                var copybookTokens = ReadCopybook(copybookName).Result;

                Continue();

                var currentIndex = tokenIndex;

                CompilerContext.SourceTokens.RemoveRange(statementIndex, currentIndex - statementIndex);

                CompilerContext.SourceTokens.InsertRange(statementIndex, copybookTokens);
                
            }
        }

        Token Current()
        {
            return sourceTokens[tokenIndex];
        }

        bool CurrentEquals(string stringToCompare)
        {
            return Current().Value.Equals(stringToCompare, StringComparison.OrdinalIgnoreCase);
        }

        void Continue()
        {
            if (tokenIndex >= sourceTokens.Count - 1) return;

            tokenIndex += 1;
        }
    }

    [GeneratedRegex("""(>>[A-Z]*(-[A-Z0-9]*)*)|[a-zA-Z]+([-|_]*[a-zA-Z0-9]+)*""", RegexOptions.ExplicitCapture | RegexOptions.NonBacktracking | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PreprocessorRegex();
}
