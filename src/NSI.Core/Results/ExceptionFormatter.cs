using System.Text;

namespace NSI.Core.Results;

public static class ExceptionFormatter {

  private const int StackPrefixLength = 3;
  private const int ExceptionNestLevel = 5;

  public static string FormatForHttp(this Exception ex) {
    var sb = new StringBuilder();

    for (var depth = 0; ex != null && depth < ExceptionNestLevel; depth++, ex = ex.InnerException!) {
      var indent = new string(' ', depth * 2);

      // [Type] Message
      sb.Append(indent)
        .Append('[').Append(ex.GetType().Name).Append("] ")
        .AppendLine(ex.Message ?? string.Empty);

      // Only the first stack frame for the root exception
      if (depth == 0 && !string.IsNullOrEmpty(ex.StackTrace)) {
        var relevantStack = GetRelevantStackTrace(ex.StackTrace!);
        if (!string.IsNullOrEmpty(relevantStack)) {
          sb.Append(indent)
            .Append("Stack: ")
            .AppendLine(relevantStack);
        }
      }
    }

    return sb.ToString().TrimEnd();
  }

  private static string? GetRelevantStackTrace(string stackTrace) {
    // Take only the first line (most relevant)
    var firstNewLine = stackTrace.IndexOf('\n', StringComparison.Ordinal);
    var firstLine = (firstNewLine >= 0 ? stackTrace[..firstNewLine] : stackTrace).Trim();

    if (firstLine.StartsWith("at ", StringComparison.Ordinal) && firstLine.Length > StackPrefixLength) {
      firstLine = firstLine[StackPrefixLength..];
    }

    return CleanFilePath(firstLine);
  }

  private static string? CleanFilePath(string? stackTraceLine) {
    if (string.IsNullOrEmpty(stackTraceLine)) {
      return stackTraceLine;
    }

    // Look for " in " before the file path
    const string inToken = " in ";
    var inIndex = stackTraceLine.LastIndexOf(inToken, StringComparison.Ordinal);
    if (inIndex == -1) {
      return stackTraceLine;
    }

    // Left part: method and parameters
    var methodPart = stackTraceLine[..inIndex];

    // Right part: path and line
    var pathPart = stackTraceLine[(inIndex + inToken.Length)..];

    // Keep only the file name (strip directories)
    var lastBackslash = pathPart.LastIndexOf('\\');
    var lastSlash = pathPart.LastIndexOf('/');
    var lastSeparatorIndex = Math.Max(lastBackslash, lastSlash);

    if (lastSeparatorIndex != -1 && lastSeparatorIndex + 1 < pathPart.Length) {
      var fileName = pathPart[(lastSeparatorIndex + 1)..];
      return $"{methodPart}{inToken}{fileName}";
    }

    // If no separator found, return original line
    return stackTraceLine;
  }
}
