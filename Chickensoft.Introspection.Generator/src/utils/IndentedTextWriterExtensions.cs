namespace Chickensoft.Introspection.Generator.Utils;

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

public static class IndentedTextWriterExtensions {
  public static void WriteCommaSeparatedList<T>(
    this IndentedTextWriter writer,
    IEnumerable<T> items,
    Action<T> writeItem,
    bool multiline = false
  ) {
    if (multiline) {
      writer.Indent++;
    }

    var enumerator = items.GetEnumerator();
    if (!enumerator.MoveNext()) {
      if (multiline) {
        writer.Indent--;
      }

      return;
    }

    writeItem(enumerator.Current);
    while (enumerator.MoveNext()) {
      writer.Write(", ");
      if (multiline) {
        writer.WriteLine();
      }
      writeItem(enumerator.Current);
    }

    if (multiline) {
      writer.WriteLine();
      writer.Indent--;
    }
  }
}
