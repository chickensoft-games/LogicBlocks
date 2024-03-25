namespace Chickensoft.LogicBlocks.Generator;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Chickensoft.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// This generator exists to list types in the developer's codebase for use
/// with polymorphic serialization and deserialization.
/// <br />
/// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism?pivots=dotnet-8-0#configure-polymorphism-with-the-contract-model
/// <br />
/// Additionally, JSON Serialization can be tested by disabling Reflection:
/// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation?pivots=dotnet-8-0#disable-reflection-defaults
/// <br />
/// For background on AOT/iOS Environments and STJ:
/// https://github.com/dotnet/runtime/issues/31326
/// </summary>
// [Generator]
public class LogicBlocksTypeGenerator
  : ChickensoftGenerator, IIncrementalGenerator {

  public record GenerationData(
    ImmutableHashSet<string> TypeNames,
    ImmutableHashSet<string> InstantiableTypeNames
  );

  public void Initialize(IncrementalGeneratorInitializationContext context) {
    var typeCandidates = context.SyntaxProvider.CreateSyntaxProvider(
      predicate: IsTypeCandidate,
      transform: Resolve
    ).Collect().Select((typeNames, _) => typeNames.ToImmutableHashSet());

    var abstractTypeCandidates = context.SyntaxProvider.CreateSyntaxProvider(
      predicate: IsAbstractTypeCandidate,
      transform: Resolve
    ).Collect().Select((typeNames, _) => typeNames.ToImmutableHashSet());

    var interfaceTypeCandidates = context.SyntaxProvider.CreateSyntaxProvider(
      predicate: IsInterfaceTypeCandidate,
      transform: Resolve
    ).Collect().Select((typeNames, _) => typeNames.ToImmutableHashSet());

    // Remove abstract types from the registry.
    // We only want to register instantiable types.
    var instantiableTypeCandidates = typeCandidates
      .Combine(abstractTypeCandidates)
      .Select((pair, _) => pair.Left.Except(pair.Right))
      .Combine(interfaceTypeCandidates)
      .Select((pair, _) => pair.Left.Except(pair.Right))
      .Combine(typeCandidates)
      .Select((pair, _) => new GenerationData(
        TypeNames: pair.Right,
        InstantiableTypeNames: pair.Left
      ));

    context.RegisterSourceOutput(
      source: instantiableTypeCandidates,
      action: static (
        SourceProductionContext context,
        GenerationData data
      ) => {
        var source = """
        namespace Chickensoft.LogicBlocks.Generated;

        using System.Collections.Immutable;
        using System.Linq;

        public static class TypeRegistry {
          /// <summary>
          /// Map of base types to their immediate subtypes.
          /// </summary>
          public static ImmutableDictionary<Type, ImmutableHashSet<Type>> TypesByBaseType { get; }

          static TypeRegistry() {
            var builder = ImmutableDictionary.CreateBuilder<Type, ImmutableHashSet<Type>.Builder>();

            foreach (var type in Types) {
              if (type.BaseType is not Type baseType) { continue; }

              if (builder.TryGetValue(baseType, out var existingSet)) {
                existingSet.Add(type);
              }
              else {
                var set = ImmutableHashSet.CreateBuilder<Type>();
                set.Add(type);
                builder.Add(baseType, set);
              }
            }

            TypesByBaseType = builder.ToImmutableDictionary(
              keySelector: kvp => kvp.Key,
              elementSelector: kvp => kvp.Value.ToImmutable()
            );
          }



          /// <summary>
          /// Get all the known subtypes of a given type.
          /// </summary>
          /// <param name="type">Ancestor type whose descendants should be returned.
          /// </param>
          /// <returns>All descendant types of <paramref name="type"/>.</returns>
          public static ImmutableHashSet<Type> GetDescendants(Type type) {
            var descendants = ImmutableHashSet.CreateBuilder<Type>();
            var queue = new Queue<Type>();
            queue.Enqueue(type);

            while (queue.Count > 0) {
              var current = queue.Dequeue();
              descendants.Add(current);

              if (TypesByBaseType.TryGetValue(current, out var children)) {
                foreach (var child in children) {
                  queue.Enqueue(child);
                }
              }
            }

            descendants.Remove(type);

            return descendants.ToImmutable();
          }

        """;

        source += CreateTypeSetProperty("Types", data.TypeNames);
        source += CreateTypeSetProperty("InstantiableTypes", data.InstantiableTypeNames);

        source += "}";

        context.AddSource(
          hintName: $"LogicBlocks.Generated.TypesRegistry.g.cs",
          source: source
        );
      }
    );
  }

  private static string CreateTypeSetProperty(
    string propertyName, ImmutableHashSet<string> typeNames
  ) => $"public static ImmutableArray<Type> {propertyName}" +
    " { get; } = new Type[] {\n" +
    AddTypeEntries(typeNames) +
    """
    }.ToImmutableArray();


    """;

  private static string AddTypeEntries(ImmutableHashSet<string> typeNames) {
    var i = 0;
    var sb = new StringBuilder();
    foreach (var typeName in typeNames.OrderBy(t => t)) {
      var isLast = i == typeNames.Count - 1;
      sb.Append($"    typeof({typeName}){(isLast ? "" : ",")}\n");
      i++;
    }
    return sb.ToString();
  }

  // Register all non-static, non-generic type declarations.
  // We'll capture a lot more than just logic block classes, but that's fine.
  // We don't need symbol data to do this, so it should be very fast.
  public static bool IsTypeCandidate(SyntaxNode node, CancellationToken _) =>
      node is TypeDeclarationSyntax typeDecl &&
      IsVisibleAndNonStatic(typeDecl) && IsNonGeneric(typeDecl);

  public static bool IsAbstractTypeCandidate(
    SyntaxNode node, CancellationToken ct
  ) =>
    node is TypeDeclarationSyntax typeDecl &&
    IsVisibleAndNonStatic(typeDecl) &&
    IsNonGeneric(typeDecl) &&
    IsAbstract(typeDecl);

  public static bool IsInterfaceTypeCandidate(
    SyntaxNode node, CancellationToken _
  ) =>
    node is InterfaceDeclarationSyntax interfaceDecl &&
    IsVisibleAndNonStatic(interfaceDecl);

  private static bool IsVisibleAndNonStatic(TypeDeclarationSyntax typeDecl) =>
    !typeDecl.Modifiers.Any(m =>
      m.IsKind(SyntaxKind.StaticKeyword) ||
      m.IsKind(SyntaxKind.PrivateKeyword) ||
      m.IsKind(SyntaxKind.ProtectedKeyword)
    );

  private static bool IsAbstract(TypeDeclarationSyntax typeDecl) =>
    typeDecl.Modifiers.Any(SyntaxKind.AbstractKeyword);

  private static bool IsNonGeneric(TypeDeclarationSyntax typeDecl) =>
    typeDecl.TypeParameterList is null or { Parameters.Count: 0 };

  // Use syntax information to construct a cheap version of the fully qualified
  // name.
  public static string Resolve(
    GeneratorSyntaxContext context, CancellationToken _
  ) => GetFullName((TypeDeclarationSyntax)context.Node);

  public static string SanitizeTypeName(string typeName) =>
      typeName.Replace('.', '_').Replace('<', '_').Replace('>', '_');

  public const char NESTED_CLASS_DELIMITER = '.';
  public const char NAMESPACE_CLASS_DELIMITER = '.';
  public const char TYPE_PARAMETER_OPEN_DELIMITER = '<';
  public const char TYPE_PARAMETER_CLOSE_DELIMITER = '>';

  /// <summary>
  /// Resolves fully qualified name using syntax nodes only for performance
  /// considerations, given that symbol resolution is slow.
  /// <br />
  /// https://stackoverflow.com/a/61409409
  /// </summary>
  /// <param name="source">Type declaration syntax.</param>
  /// <returns>Fully qualified name.</returns>
  /// <exception cref="ArgumentNullException />
  public static string GetFullName(TypeDeclarationSyntax source) {
    if (source is null) {
      throw new ArgumentNullException(nameof(source));
    }

    var namespaces = new LinkedList<BaseNamespaceDeclarationSyntax>();
    var types = new LinkedList<TypeDeclarationSyntax>();
    for (
      var parent = source.Parent; parent is not null; parent = parent.Parent
    ) {
      if (parent is BaseNamespaceDeclarationSyntax @namespace) {
        namespaces.AddFirst(@namespace);
      }
      else if (parent is TypeDeclarationSyntax type) {
        types.AddFirst(type);
      }
    }

    var result = new StringBuilder();
    for (var item = namespaces.First; item is not null; item = item.Next) {
      result.Append(item.Value.Name).Append(NAMESPACE_CLASS_DELIMITER);
    }
    for (var item = types.First; item is not null; item = item.Next) {
      var type = item.Value;
      AppendName(result, type);
      result.Append(NESTED_CLASS_DELIMITER);
    }
    AppendName(result, source);

    return result.ToString();
  }

  private static void AppendName(
    StringBuilder builder, TypeDeclarationSyntax type
  ) {
    builder.Append(type.Identifier.Text);
    var typeArguments = type.TypeParameterList?.ChildNodes()
        .Count(node => node is TypeParameterSyntax) ?? 0;
    if (typeArguments != 0) {
      builder
        .Append(TYPE_PARAMETER_OPEN_DELIMITER)
        .Append(typeArguments)
        .Append(TYPE_PARAMETER_CLOSE_DELIMITER);
    }
  }
}
