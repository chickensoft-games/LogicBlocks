namespace Chickensoft.LogicBlocks.Generator;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Chickensoft.LogicBlocks.Generator.Types.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// This generator exists to list types in the developer's codebase for use
/// with polymorphic serialization and deserialization or automatic state
/// creation and registration.
/// <br />
/// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism?pivots=dotnet-8-0#configure-polymorphism-with-the-contract-model
/// <br />
/// Additionally, JSON Serialization can be tested by disabling Reflection:
/// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation?pivots=dotnet-8-0#disable-reflection-defaults
/// <br />
/// For background on AOT/iOS Environments and STJ:
/// https://github.com/dotnet/runtime/issues/31326
/// </summary>
[Generator]
public class TypeGenerator : IIncrementalGenerator {

  public void Initialize(IncrementalGeneratorInitializationContext context) {
    // If you need to debug the source generator, uncomment the following line
    // and use Visual Studio 2022 on Windows to attach to debugging next time
    // the source generator process is started by running `dotnet build` in
    // the project consuming the source generator
    //
    // --------------------------------------------------------------------- //
    // System.Diagnostics.Debugger.Launch();
    // --------------------------------------------------------------------- //
    //
    // You can debug a source generator in Visual Studio on Windows by
    // simply uncommenting the Debugger.Launch line above.

    // Otherwise...
    // To debug on macOS with VSCode, you can pull open the command palette
    // and select "Debug: Attach to a .NET 5+ or .NET Core process"
    // (csharp.attachToProcess) and then search "VBCS" and select the
    // matching compiler process. Once it attaches, this will stop sleeping
    // and you're on your merry way!

    // --------------------------------------------------------------------- //
    // while (!System.Diagnostics.Debugger.IsAttached) {
    //   Thread.Sleep(500);
    // }
    // System.Diagnostics.Debugger.Break();
    // --------------------------------------------------------------------- //

    // Because of partial type declarations, we may need to combine some
    // type declarations into one.
    var incrementalGenerationData = context.SyntaxProvider.CreateSyntaxProvider(
      predicate: IsTypeCandidate,
      transform: Resolve
    )
    .Collect()
    .Select((declaredTypes, _) => {
      var declaredTypesByFullName = declaredTypes
        .GroupBy((t) => t.FullName)
        .Select(
          // Combine non-unique type entries together.
          g => g.Aggregate((a, b) => a.Combine(b))
        )
        // Map type declarations by their full name for fast lookup.
        .ToDictionary(
          g => g.FullName,
          g => g
        );

      var tree = new TypeResolutionTree();
      tree.AddDeclaredTypes(declaredTypesByFullName);

      var visibleTypes = tree.GetVisibleTypes();
      var visibleInstantiableTypes = tree.GetVisibleTypes(
        predicate:
          static (type) => type.IsInstantiable && type.OpenGenerics == "",
        searchGenericTypes: false
      );

      var generationData = new GenerationData(
        VisibleTypes: visibleTypes.ToImmutableHashSet(),
        VisibleInstantiableTypes: visibleInstantiableTypes.ToImmutableHashSet()
      );

      return generationData;
    });

    context.RegisterSourceOutput(
      source: incrementalGenerationData,
      action: static (
        SourceProductionContext context,
        GenerationData data
      ) => {
        var source = """
        public class TypeRegistry : Chickensoft.LogicBlocks.Types.ITypeRegistry {

        """;

        // Sort types for deterministic output.
        var orderedTypes = data.VisibleTypes.OrderBy(t => t).ToList();
        var instantiableTypes =
          data.VisibleInstantiableTypes.OrderBy(t => t).ToList();

        source += CreateVisibleTypesProperty(orderedTypes);
        source += CreateVisibleInstantiableTypesProperty(instantiableTypes);
        source += "}";

        context.AddSource(
          hintName: $"TypeRegistry.g.cs",
          source: source
        );
      }
    );
  }

  public static DeclaredTypeInfo Resolve(
    GeneratorSyntaxContext context, CancellationToken _
  ) {
    var typeDecl = (TypeDeclarationSyntax)context.Node;
    var location = GetLocation(typeDecl);
    var kind = GetKind(typeDecl);
    var name = typeDecl.Identifier.ValueText;
    var isVisible = IsVisible(typeDecl);
    var numTypeParameters = typeDecl.TypeParameterList?.Parameters.Count() ?? 0;

    return new DeclaredTypeInfo(
      location, kind, isVisible, numTypeParameters, name
    );
  }

  private static string CreateVisibleTypesProperty(
    IList<string> typeNames
  ) =>
    "  public System.Collections.Generic.ISet<System.Type> VisibleTypes" +
    " { get; } = new System.Collections.Generic.HashSet<System.Type>() {\n" +
    AddTypeEntries(typeNames) +
    """
      };


    """;

  private static string CreateVisibleInstantiableTypesProperty(
    IList<string> typeNames
  ) =>
    "  public System.Collections.Generic.IDictionary" +
    "<System.Type, System.Func<object>> VisibleInstantiableTypes" +
    " { get; } = new System.Collections.Generic.Dictionary" +
    "<System.Type, System.Func<object>>() {\n" +
    AddInstantiableTypeEntries(typeNames) +
    """
      };


    """;

  private static string AddTypeEntries(IList<string> typeNames) {
    var i = 0;
    var sb = new StringBuilder();
    foreach (var typeName in typeNames.OrderBy(t => t)) {
      var isLast = i == typeNames.Count - 1;
      sb.Append($"    typeof({typeName}){(isLast ? "" : ",")}\n");
      i++;
    }
    return sb.ToString();
  }

  private static string AddInstantiableTypeEntries(IList<string> typeNames) {
    var i = 0;
    var sb = new StringBuilder();
    foreach (var typeName in typeNames.OrderBy(t => t)) {
      var isLast = i == typeNames.Count - 1;
      sb.Append(
        $"    [typeof({typeName})] = () => " +
        $"System.Activator.CreateInstance<{typeName}>()" +
        $"{(isLast ? "" : ",")}\n"
      );
      i++;
    }
    return sb.ToString();
  }

  // We identify all type declarations and filter them out later by visibility
  // based on all the information about the type from any partial declarations
  // of the same type that we discover, as well as visibility information about
  // any containing types.
  public static bool IsTypeCandidate(SyntaxNode node, CancellationToken _) =>
      node is TypeDeclarationSyntax;

  public static DeclaredTypeKind GetKind(TypeDeclarationSyntax typeDecl) {
    if (typeDecl.Modifiers.Any(SyntaxKind.AbstractKeyword)) {
      // We know abstract types aren't interfaces or static classes.
      return DeclaredTypeKind.AbstractType;
    }
    if (typeDecl is ClassDeclarationSyntax classDecl) {
      return classDecl.Modifiers.Any(SyntaxKind.StaticKeyword)
        ? DeclaredTypeKind.StaticClass
        : DeclaredTypeKind.InstantiableType;
    }
    else if (typeDecl is InterfaceDeclarationSyntax) {
      return DeclaredTypeKind.Interface;
    }
    return DeclaredTypeKind.InstantiableType;
  }

  /// <summary>
  /// True if the type declaration is explicitly marked as visible at the
  /// top-level of the project.
  /// </summary>
  /// <param name="typeDecl">Type declaration syntax.</param>
  /// <returns>True if marked as `public` or `internal`.</returns>
  public static bool IsVisible(TypeDeclarationSyntax typeDecl) =>
    typeDecl.Modifiers.Any(m =>
      m.IsKind(SyntaxKind.PublicKeyword) ||
      m.IsKind(SyntaxKind.InternalKeyword)
    );

  /// <summary>
  /// Determines where a type is located within the source code.
  /// <br />
  /// https://stackoverflow.com/a/61409409
  /// </summary>
  /// <param name="source">Type declaration syntax.</param>
  /// <returns>Fully qualified name.</returns>
  /// <exception cref="ArgumentNullException />
  public static TypeLocation GetLocation(TypeDeclarationSyntax source) {
    var namespaces = new LinkedList<string>();
    var types = new LinkedList<string>();
    for (
      var parent = source.Parent; parent is not null; parent = parent.Parent
    ) {
      if (parent is BaseNamespaceDeclarationSyntax @namespace) {
        foreach (var namespacePart in @namespace.Name.ToString().Split('.').Reverse()) {
          namespaces.AddFirst(namespacePart);
        }
      }
      else if (parent is TypeDeclarationSyntax type) {
        types.AddFirst(type.Identifier.ValueText);
      }
    }

    return new TypeLocation(namespaces, types);
  }
}
