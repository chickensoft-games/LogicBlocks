namespace Chickensoft.SourceGeneratorUtils;

using System;
using System.Text.RegularExpressions;

public static class Extensions
{
  /// <summary>
  /// Normalizes line endings to '\n' or your endings.
  /// Borrowed from https://github.com/HavenDV/H.Generators.Extensions (MIT)
  /// </summary>
  /// <param name="text">Text to normalize.</param>
  /// <param name="newLine">'\n' by default</param>
  /// <returns>String with normalized line endings.</returns>
  /// <exception cref="ArgumentNullException"></exception>
  public static string NormalizeLineEndings(
    this string text,
    string? newLine = null
  )
  {
    newLine ??= Environment.NewLine;
    return text
      .Replace("\r\n", "\n")
      .Replace("\r", "\n")
      .Replace("\n", newLine);
  }

  /// <summary>
  /// Replaces white-space only lines with empty lines and replaces subsequent
  /// spans of empty lines at least 3 long with a single empty line.
  /// </summary>
  /// <param name="text">Text to clean.</param>
  /// <param name="newLine">Newline character. Leave blank for environment
  /// default.</param>
  /// <returns>Cleaned string.</returns>
  public static string Clean(this string text, string? newLine = null)
  {
    newLine ??= Environment.NewLine;
    var value = text.NormalizeLineEndings();

    var lines = value.Split([newLine], StringSplitOptions.None);
    for (var i = 0; i < lines.Length; i++)
    {
      lines[i] = string.IsNullOrWhiteSpace(lines[i]) ? "" : lines[i];
    }

    var escaped = Regex.Escape(newLine);
    var regex = new Regex($$"""({{escaped}}){3,}""");
    return regex.Replace(string.Join(newLine, lines), newLine);
  }
}
