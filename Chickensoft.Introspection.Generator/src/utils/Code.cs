
namespace Chickensoft.Introspection.Generator.Utils;

using System;
using System.Text.RegularExpressions;

public static class Code {
  // Handwritten regex for nameof() expressions. Group 1 is what you want.
  public const string NAME_OF = @"(?:(?<=\.)?([^.<>\n]*)(?:<[^.\n]+>)?(?=$))";
  public static Regex NameOfRegex { get; } = new Regex(NAME_OF);

  private static readonly char[] _nameofChars = "nameof(".ToCharArray();

  /// <summary>
  /// Compute the nameof expression for a given string.
  /// </summary>
  /// <param name="input">Input text.</param>
  /// <returns>Equivalent value that nameof() would produce.</returns>
  public static string NameOf(string input) {
    var text = input.StartsWith("nameof(") ?
      input.TrimStart(_nameofChars).TrimEnd(')')
      : input;

    var match = NameOfRegex.Match(text);

    return match.Success ? match.Groups[1].Value : text;
  }

  /// <summary>
  /// Converts a PascalCase string to snake_case. Oddly involved to
  /// properly handle all caps abbreviations and acronyms in PascalCaseLIKEThis.
  /// </summary>
  /// <param name = "input">Input string.</param>
  /// <returns>String converted to snake_case.</returns>
  public static string PascalCaseToSnakeCase(string input) {
    if (string.IsNullOrEmpty(input)) {
      return input;
    }

    var span = input.AsSpan();

    // Worst case is twice as many characters (underscore after each letter)
    Span<char> output = stackalloc char[span.Length * 2];
    var outputIndex = 0;

    var wasUppercase = false;

    for (var i = 0; i < span.Length; i++) {
      var c = span[i];
      var isUppercase = c is >= 'A' and <= 'Z';
      var lower = isUppercase ? (char)(c | 0x20) : c;
      var nextIsUppercase = i + 1 < span.Length &&
        span[i + 1] is >= 'A' and <= 'Z';

      if (!isUppercase && nextIsUppercase) {
        // output underscore and lowercase letter
        output[outputIndex++] = lower;

        if (i > 0) {
          output[outputIndex++] = '_';
        }
      }
      else if (!wasUppercase && !isUppercase && nextIsUppercase) {
        // output underscore and lowercase letter
        output[outputIndex++] = lower;

        if (i < span.Length - 1) {
          output[outputIndex++] = '_';
        }
      }
      else if (wasUppercase && isUppercase && !nextIsUppercase) {
        // output underscore and lowercase letter
        if (i < span.Length - 1) {
          output[outputIndex++] = '_';
        }

        output[outputIndex++] = lower;
      }
      else if (isUppercase && wasUppercase) {
        // output lowercase letter
        output[outputIndex++] = lower;
      }
      else {
        // output letter
        output[outputIndex++] = lower;
      }

      wasUppercase = isUppercase;
    }

    return output.Slice(0, outputIndex).ToString();
  }
}
