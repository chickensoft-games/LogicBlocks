namespace Chickensoft.SourceGeneratorUtils;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Simple, but effective.
/// Inspired by https://dev.to/panoukos41/debugging-c-source-generators-1flm.
/// </summary>
public class Log {
  protected List<string> Logs { get; } = [];

  public void Print(string msg) {
#if DEBUG
    var lines = msg.Split('\n').Select(line => "//\t" + line);
    Logs.AddRange(lines);
#endif
  }

  public void Clear() => Logs.Clear();
  public string Contents => string.Join(Environment.NewLine, Logs);
}
