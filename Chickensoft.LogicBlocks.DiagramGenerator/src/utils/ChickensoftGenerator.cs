namespace Chickensoft.SourceGeneratorUtils;

using System.Collections.Generic;
using System.Linq;

public abstract class ChickensoftGenerator {
  /// <summary>
  /// Produces whitespace for the required number of tabs.
  /// </summary>
  /// <param name="numTabs">Indentation level.</param>
  /// <returns><paramref name="numTabs" /> * <see cref="SPACES_PER_TAB"/>
  /// spaces in a string.</returns>
  public static string Tab(int numTabs)
  => new(' ', numTabs * Constants.SPACES_PER_INDENT);

  /// <summary>Indents the given text by the given number of tabs.</summary>
  /// <param name="numTabs">Indentation level.</param>
  /// <param name="text">Text to indent.</param>
  /// <returns>Indented text.</returns>
  public static string Tab(int numTabs, string text) => Tab(numTabs) + text;

  /// <summary>
  /// Normalizes the new lines and uses a custom indentation-aware interpolation
  /// handler to preserve proper indentations for string enumerable expressions
  /// inside the interpolation.
  /// </summary>
  /// <param name="code">Code to format.</param>
  /// <returns>"Formatted" code.</returns>
  public static string Format(IndentationAwareInterpolationHandler code)
    => code.GetFormattedText().Clean();

  /// <summary>
  /// Returns the given <paramref name="lines" /> of code if
  /// <paramref name="condition" /> is true, otherwise returns
  /// an empty enumerable.
  /// </summary>
  /// <param name="condition">Condition to check.</param>
  /// <param name="lines">Lines of code to return if condition is true.</param>
  /// <returns>Enumerable lines of code.</returns>
  public static IEnumerable<string> If(
    bool condition, IEnumerable<string> lines
  ) => condition ? lines : [];

  /// <summary>
  /// Returns the given <paramref name="lines" /> of code if
  /// <paramref name="condition" /> is true, otherwise returns
  /// an empty enumerable.
  /// </summary>
  /// <param name="condition">Condition to check.</param>
  /// <param name="lines">Lines of code to return if condition is true.</param>
  /// <returns>Enumerable lines of code.</returns>
  public static IEnumerable<string> If(
    bool condition, params string[] lines
  ) => condition ? lines : Enumerable.Empty<string>();
}
