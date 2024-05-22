namespace Chickensoft.Introspection.Generator.Models;

using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Linq;
using Chickensoft.Introspection.Generator.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public record GenericTypeNode(
  string Type,
  ImmutableArray<GenericTypeNode> Children
) {
  /// <summary>
  /// Name of the type, including any open generics portion of the name (if the
  /// type is generic) — i.e., the open generic type.
  /// </summary>
  public string OpenType =>
    Type + TypeReference.GetOpenGenerics(Children.Length);

  /// <summary>
  /// Name of the type, including any generic type arguments — i.e., the closed
  /// generic type.
  /// </summary>
  public string ClosedType => Type + TypeReference.GetGenerics(
    Children.Select(child => child.ClosedType).ToImmutableArray()
  );

  /// <summary>
  /// Recursively constructs a generic type node from a generic name syntax.
  /// </summary>
  /// <param name="genericName">Generic name syntax.</param>
  /// <returns>Generic type node tree.</returns>
  public static GenericTypeNode Create(GenericNameSyntax genericName) {
    var type = genericName.Identifier.NormalizeWhitespace().ToString();

    var children = genericName.TypeArgumentList.Arguments
      .Select(arg => arg switch {
        GenericNameSyntax genericNameSyntax => Create(genericNameSyntax),
        _ => new GenericTypeNode(
          arg.NormalizeWhitespace().ToString(),
          ImmutableArray<GenericTypeNode>.Empty
        )
      })
      .ToImmutableArray();

    return new GenericTypeNode(type, children);
  }

  public void Write(IndentedTextWriter writer) {
    var openType = Type + TypeReference.GetOpenGenerics(Children.Length);
    var closedType = Type + TypeReference.GetGenerics(
      Children.Select(child => child.ClosedType).ToImmutableArray()
    );

    writer.WriteLine("new GenericType(");
    writer.Indent++;
    writer.WriteLine($"OpenType: typeof({openType.TrimEnd('?')}),");
    writer.WriteLine($"ClosedType: typeof({closedType.TrimEnd('?')}),");

    if (Children.Length > 0) {
      writer.WriteLine("Arguments: new GenericType[] {");
      writer.Indent++;

      writer.WriteCommaSeparatedList(
        Children,
        child => child.Write(writer),
        multiline: true
      );

      writer.Indent--;
      writer.WriteLine("},");
    }
    else {
      writer.WriteLine("Arguments: System.Array.Empty<GenericType>(),");
    }

    writer.WriteLine(
      "GenericTypeGetter: receiver => " +
      $"receiver.Receive<{closedType}>(),"
    );
    if (Children.Length >= 2) {
      writer.WriteLine(
        "GenericTypeGetter2: receiver => " +
        $"receiver.Receive<{Children[0].ClosedType}, {Children[1].ClosedType}>()"
      );
    }
    else {
      writer.WriteLine("GenericTypeGetter2: default");
    }
    writer.Indent--;
    writer.Write(")");
  }
}
