namespace Chickensoft.Introspection.Generator.Models;

using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Linq;
using Chickensoft.Introspection.Generator.Utils;

/// <summary>
/// Represents an attribute applied to a property.
/// </summary>
/// <param name="Name">Name of the attribute.</param>
/// <param name="ConstructorArgs">Attribute constructor arguments.</param>
/// <param name="InitializerArgs">Attribute initializer arguments (not part
/// of the constructor signature, but settable properties from object
/// initializer syntax).</param>
public sealed record DeclaredAttribute(
  string Name,
  ImmutableArray<string> ConstructorArgs,
  ImmutableArray<string> InitializerArgs
) {
  public static void WriteAttributeMap(
    IndentedTextWriter writer,
    ImmutableArray<DeclaredAttribute> attributeUsages
  ) {
    var attributesByName = attributeUsages
      .GroupBy(attr => attr.Name)
      .ToDictionary(
        group => group.Key,
        group => group.ToImmutableArray()
      );

    writer.WriteCommaSeparatedList(
      attributesByName.Keys.OrderBy(a => a), // Sort for deterministic output.
      (attributeName) => {
        var attributes = attributesByName[attributeName];

        writer.WriteLine(
          $"[typeof({attributeName}Attribute)] = new System.Attribute[] {{"
        );

        writer.WriteCommaSeparatedList(
          attributes, // Respect the order they were applied.
          (attribute) => attribute.Write(writer),
          multiline: true
        );

        writer.Write("}");
      },
      multiline: true
    );
  }

  private void Write(IndentedTextWriter writer) {
    writer.Write($"new {Name}Attribute(");
    writer.Write(string.Join(", ", ConstructorArgs));
    writer.Write(")");
    if (InitializerArgs.Length > 0) {
      writer.Write(" { ");
      writer.Write(string.Join(", ", InitializerArgs));
      writer.Write(" }");
    }
  }

  public bool Equals(DeclaredAttribute? other) =>
    other is not null &&
    Name == other.Name &&
    ConstructorArgs.SequenceEqual(other.ConstructorArgs) &&
    InitializerArgs.SequenceEqual(other.InitializerArgs);

  public override int GetHashCode() => HashCode.Combine(
    Name,
    ConstructorArgs,
    InitializerArgs
  );
}
