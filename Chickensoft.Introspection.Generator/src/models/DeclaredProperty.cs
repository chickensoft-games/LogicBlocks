namespace Chickensoft.Introspection.Generator.Models;

using System.CodeDom.Compiler;
using System.Collections.Immutable;

/// <summary>
/// Represents a property on a metatype. Properties are opt-in and persisted.
/// </summary>
/// <param name="Name">Name of the </param>
/// <param name="HasSetter">True if the property has a setter.</param>
/// <param name="IsNullable">True if the property is nullable.</param>
/// <param name="GenericType">Type of the </param>
/// <param name="Attributes">Attributes applied to the </param>
public record DeclaredProperty(
  string Name,
  bool HasSetter,
  bool IsInit,
  bool IsNullable,
  GenericTypeNode GenericType,
  ImmutableArray<DeclaredAttribute> Attributes
) {
  public void Write(IndentedTextWriter writer, string typeSimpleNameClosed) {
    writer.WriteLine($"new {Constants.PROPERTY_METADATA}(");
    writer.Indent++;

    var propertyValue = "value" + (IsNullable ? "" : "!");

    var setter = HasSetter
      ? (
        IsInit
          ? "(object obj, object? value) => throw new " +
          "System.InvalidOperationException(" +
          $"\"Property {Name} on {typeSimpleNameClosed} is init-only.\"" +
          ")"
          : $"(object obj, object? value) => (({typeSimpleNameClosed})obj)" +
          $".{Name} = ({GenericType.ClosedType}){propertyValue}"
      )
      : "(object obj, object? _) => { }";

    writer.WriteLine($"Name: \"{Name}\",");
    writer.WriteLine($"IsInit: {(IsInit ? "true" : "false")},");
    writer.WriteLine(
      $"Getter: (object obj) => (({typeSimpleNameClosed})obj).{Name},"
    );
    writer.WriteLine($"Setter: {setter},");
    writer.Write("GenericType: ");
    GenericType.Write(writer);
    writer.WriteLine(",");
    writer.WriteLine(
      "Attributes: new System.Collections.Generic.Dictionary" +
      "<System.Type, System.Attribute[]>() {"
    );

    DeclaredAttribute.WriteAttributeMap(writer, Attributes);

    writer.WriteLine("}");

    writer.Indent--;

    writer.Write(")");
  }
}
