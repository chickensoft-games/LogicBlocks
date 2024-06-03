namespace Chickensoft.Introspection.Generator.Models;

using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Linq;
using Chickensoft.Introspection.Generator.Utils;

public class DeclaredTypeRegistry {
  public ImmutableArray<UsingDirective> GlobalUsings { get; init; }
  public ScopeTree ScopeTree { get; init; }
  public ImmutableDictionary<string, DeclaredType> AllTypes { get; init; }
  public ImmutableHashSet<DeclaredType> VisibleTypes { get; init; }

  public DeclaredTypeRegistry(
    ImmutableArray<UsingDirective> globalUsings,
    ScopeTree scopeTree,
    ImmutableDictionary<string, DeclaredType> allTypes,
    ImmutableHashSet<DeclaredType> visibleTypes
  ) {
    GlobalUsings = globalUsings;
    ScopeTree = scopeTree;
    AllTypes = allTypes;
    VisibleTypes = visibleTypes;
  }

  public override int GetHashCode() => HashCode.Combine(
    GlobalUsings, ScopeTree, AllTypes, VisibleTypes
  );

  public override bool Equals(object? obj) =>
    obj is DeclaredTypeRegistry data &&
    GlobalUsings.SequenceEqual(data.GlobalUsings) &&
    AllTypes.SequenceEqual(data.AllTypes) &&
    VisibleTypes.SequenceEqual(data.VisibleTypes);

  public void Write(IndentedTextWriter writer) {
    writer.WriteLine(
      "public partial class TypeRegistry : " +
      $"{Constants.TYPE_REGISTRY_INTERFACE} {{"
    );

    writer.Indent++;
    writer.WriteLine(
      $"public static {Constants.TYPE_REGISTRY_INTERFACE} Instance " +
      "{ get; } = new TypeRegistry();"
    );
    writer.WriteLine();

    // Visible types property
    // ----------------------------------------------------------------- //
    writer.WriteLine(
      "public System.Collections.Generic.IReadOnlyDictionary" +
      "<System.Type, Chickensoft.Introspection.ITypeMetadata> " +
      "VisibleTypes { get; } = new System.Collections.Generic.Dictionary" +
      "<System.Type, Chickensoft.Introspection.ITypeMetadata>() {"
    );

    writer.Indent++;
    writer.WriteCommaSeparatedList(
      VisibleTypes
        .Where(
          type => type.Kind is
            DeclaredTypeKind.AbstractType or
            DeclaredTypeKind.ConcreteType
          )
        .OrderBy(type => type.FullNameOpen), // Sort for deterministic output.
      (type) => {
        var knownToBeAccessibleFromGlobalScope = VisibleTypes.Contains(type);
        writer.Write($"[typeof({type.FullNameOpen})] = ");
        type.WriteMetadata(writer, knownToBeAccessibleFromGlobalScope);
      },
      multiline: true
    );
    writer.Indent--;
    writer.WriteLine("};");
    // ----------------------------------------------------------------- //

    writer.WriteLine();

    // Module initializer that automatically registers types.
    // ----------------------------------------------------------------- //
    writer.WriteLine("[System.Runtime.CompilerServices.ModuleInitializer]");
    writer.WriteLine(
      "internal static void Initialize() => " +
      $"{Constants.TYPES_GRAPH}.Register(TypeRegistry.Instance);"
    );

    writer.Indent--;
    writer.WriteLine("}");

    writer.WriteLine();
  }
}
