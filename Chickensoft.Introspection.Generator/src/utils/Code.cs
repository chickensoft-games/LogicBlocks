
namespace Chickensoft.Introspection.Generator.Utils;

using System.Text.RegularExpressions;

public static class Code {
  // Handwritten regex for nameof() expressions. Group 1 is what you want.
  public const string NAME_OF = @"(?:(?<=\.)?([^.<>\n]*)(?:<[^.\n]+>)?(?=$))";
  public static Regex NameOfRegex { get; set; } = new Regex(NAME_OF);

  /// <summary>
  /// Compute the nameof expression for a given string.
  /// </summary>
  /// <param name="input">Input text.</param>
  /// <returns>Equivalent value that nameof() would produce.</returns>
  public static string NameOf(string input) {
    var text = input.StartsWith("nameof(") ?
      input.Substring(7, input.Length - 8)
      : input;

    var match = NameOfRegex.Match(text);

    return match.Success ? match.Groups[1].Value : text;
  }
}
