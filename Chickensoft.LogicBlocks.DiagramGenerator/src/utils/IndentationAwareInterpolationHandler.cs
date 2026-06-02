namespace Chickensoft.SourceGeneratorUtils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

/// <summary>
/// Custom string interpolation handler. When string enumerable expressions
/// are interpolated inside a string, the handler will automatically indent
/// subsequent lines to match the indent where the first line occurs.
/// </summary>
[InterpolatedStringHandler]
public readonly ref struct IndentationAwareInterpolationHandler
{
  private class State
  {
    public bool EndedOnWhitespace { get; set; }
    public int Indent { get; set; }
    public string Prefix => new(' ', Indent * Constants.SPACES_PER_INDENT);
  }

  private readonly StringBuilder _sb;
  private readonly State _state = new();

  public IndentationAwareInterpolationHandler(
    int literalLength, int formattedCount
  )
  {
    _sb = new StringBuilder(literalLength);
  }

  public void AppendLiteral(string s) => AddString(s);

  public void AppendFormatted<T>(T? t)
  {
    switch (t)
    {
      case IEnumerable<string> lines:
        AddLines(lines);
        break;
      case string str:
        AddString(str);
        break;
      case not null:
        _sb.Append(t);
        break;
    }
  }

  private void AddString(string s)
  {
    var value = s.NormalizeLineEndings(); //Replace CRLF with LF
    var lastNewLineIndex = value.LastIndexOf('\n'); //Find the last newline character
    var remainingString = value.Substring(lastNewLineIndex + 1); //Everything after the last newline
    var remainingNonWs = remainingString.TrimEnd(); //Everything after the last newline, trimmed of whitespace
    _state.EndedOnWhitespace = remainingNonWs.Length == 0; // true if the last line ended on whitespace
    _state.Indent = _state.EndedOnWhitespace //If the string ended on whitespace, indent by the number of spaces per indent
      ? remainingString.Length / Constants.SPACES_PER_INDENT
      : 0;
    _sb.Append(value);
  }

  private void AddLines(IEnumerable<string> lines)
  {
    // Makes subsequent lines after the first share the same initial
    // indentation amount as where the first line occurs, plus any additional
    // indent added by the line.
    var prefix = _state.Prefix;
    var value = string.Join(
      Environment.NewLine,
      lines.Take(1).Concat(lines.Skip(1).Select((line) => prefix + line))
    );
    if (string.IsNullOrEmpty(value))
    {
      return;
    }
    _sb.Append(value);
  }

  internal string GetFormattedText() => _sb.ToString();
}
