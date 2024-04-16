namespace Chickensoft.LogicBlocks.Generator;

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Chickensoft.LogicBlocks.Generator.Types;
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
      var typesByFullName = declaredTypes
        .GroupBy((type) => type.FullName);

      var uniqueTypes = typesByFullName
        .Select(
          // Combine non-unique type entries together.
          group => group.Aggregate((typeA, typeB) => typeA.MergePartialDefinition(typeB))
        )
        .OrderBy(type => type.FullName) // Sort for deterministic output
        .ToDictionary(
          g => g.FullName,
          g => g
        );

      var tree = new TypeResolutionTree();
      tree.AddDeclaredTypes(uniqueTypes);

      var visibleTypeIds = tree.GetVisibleTypes();
      var visibleInstantiableTypeIds = tree.GetVisibleTypes(
        predicate:
          static (type) => type.IsInstantiable && type.OpenGenerics == "",
        searchGenericTypes: false
      );

      // TODO: Replace the next 3 loops with one single loop and build the
      // dictionaries manually.

      var visibleTypes = uniqueTypes.Values
        .Where(type => visibleTypeIds.Contains(type.FullName))
        .ToImmutableDictionary(
          type => type.FullName,
          type => type
        );

      var visibleInstantiableTypes = uniqueTypes
        .Values
        .Where(type => visibleInstantiableTypeIds.Contains(type.FullName))
        .ToImmutableDictionary(
          type => type.FullName,
          type => type
        );

      var metatypes = visibleTypes.Values
        .Where(
          type => type.HasMetatypeAttribute && type.CanGenerateMetatypeInfo
        )
        .ToImmutableDictionary(
          type => type.FullName,
          type => type
        );

      var generationData = new GenerationData(
        Metatypes: metatypes,
        VisibleTypes: visibleTypes,
        VisibleInstantiableTypes: visibleInstantiableTypes
      );

      return generationData;
    });

    context.RegisterSourceOutput(
      source: incrementalGenerationData,
      action: static (
        SourceProductionContext context,
        GenerationData data
      ) => {
        GenerateTypeRegistry(context, data);
        OutputMetatypesAndReportDiagnostics(context, data);
      }
    );
  }

  public static void OutputMetatypesAndReportDiagnostics(
    SourceProductionContext context,
    GenerationData data
  ) {
    // A metatype is a class generated by the source generator that contains
    // information about the class it is generated inside of.
    foreach (var type in data.VisibleTypes.Values) {
      foreach (var diagnostic in type.Diagnostics) {
        context.ReportDiagnostic(diagnostic);
      }

      if (!type.CanGenerateMetatypeInfo) {
        continue;
      }

      var usings = type.Usings
        .Where(u => !u.IsGlobal) // Globals are universally available
        .OrderBy(u => u.TypeName)
        .ThenBy(u => u.IsGlobal)
        .ThenBy(u => u.IsStatic)
        .ThenBy(u => u.IsAlias)
        .Select(@using => @using.CodeString).ToList();

      var code = CreateCodeWriter();

      code.WriteLine("#pragma warning disable");
      code.WriteLine("#nullable enable");
      code.WriteLine($"namespace {type.Location.Namespace};\n");

      foreach (var usingDirective in usings) {
        code.WriteLine(usingDirective);
      }

      if (usings.Count > 0) { code.WriteLine(); }

      // Nest it inside all the containing types
      foreach (var containingType in type.Location.ContainingTypes) {
        code.WriteLine($"{containingType.CodeString} {{");
        code.Indent++;
      }

      // Nest it inside the type itself
      code.WriteLine($"{type.Reference.CodeString} : Chickensoft.LogicBlocks.IHasMetatype {{");

      code.Indent++;
      code.WriteLine($"public record Metatype : Chickensoft.LogicBlocks.IMetatype {{");
      code.Indent++;
      OutputMetatypeInformation(type, code);
      code.Indent -= 1;


      // Close braces
      for (var i = code.Indent; i >= 0; i--) {
        code.WriteLine("}");
        code.Indent--;
      }

      code.WriteLine("#nullable restore");
      code.WriteLine("#pragma warning restore");

      context.AddSource(
        hintName: $"{type.Filename}.g.cs",
        source: code.InnerWriter.ToString()
      );
    }
  }

  private static void OutputMetatypeInformation(
    DeclaredTypeInfo type, IndentedTextWriter code
  ) {
    var fullName = type.Reference.NameWithOpenGenerics;

    code.WriteLine($"public System.Text.Json.Serialization.Metadata.JsonTypeInfo CreateJsonTypeInfo(Chickensoft.LogicBlocks.Types.ITypeRegistry registry, System.Text.Json.JsonSerializerOptions options) => Chickensoft.LogicBlocks.LogicBlockTypes.CreateJsonTypeInfo(registry, options, typeof({fullName}));");
    code.WriteLine();
    code.WriteLine($"public System.Collections.Generic.IList<Chickensoft.LogicBlocks.LogicProp> Properties {{ get; }} = new System.Collections.Generic.List<Chickensoft.LogicBlocks.LogicProp>() {{");

    for (var i = 0; i < type.Properties.Length; i++) {
      var isLast = i == type.Properties.Length - 1;

      var property = type.Properties[i];

      code.Indent++;
      GenerateLogicProp(type, property, code);
      code.Indent--;

      if (!isLast) { code.Write(","); }

      code.WriteLine();
    }

    code.WriteLine("};");
  }

  public static void GenerateLogicProp(
    DeclaredTypeInfo type, Property property, IndentedTextWriter code
  ) {
    code.WriteLine($"new Chickensoft.LogicBlocks.LogicProp(");
    code.Indent++;

    var propertyValue = "value" + (property.IsNullable ? "" : "!");

    code.WriteLine($"Name: \"{property.Name}\",");
    code.WriteLine($"Type: typeof({type.Reference.Name}),");
    code.WriteLine($"Getter: (object obj) => (({type.Reference.Name})obj).{property.Name},");
    code.WriteLine($"Setter: (object obj, object? value) => (({type.Reference.Name})obj).{property.Name} = ({property.Type}){propertyValue},");
    code.WriteLine($"AttributesByType: new System.Collections.Generic.Dictionary<System.Type, System.Attribute[]>() {{");

    var attributesByType = property.Attributes
      .GroupBy(attr => attr.Name)
      .ToDictionary(
        group => group.Key,
        group => group.ToImmutableArray()
      );

    code.Indent++;
    GenerateLogicPropAttributeMapping(attributesByType, code);
    code.Indent--;

    code.WriteLine("}");

    code.Indent--;

    code.Write(")");
  }

  public static void GenerateLogicPropAttributeMapping(
    Dictionary<string, ImmutableArray<PropertyAttribute>> attributes,
    IndentedTextWriter code
  ) {
    var i = 0;

    foreach (var attributeKey in attributes.Keys) {
      var attributeEntries = attributes[attributeKey];
      var isLast = i == attributes.Count - 1;

      code.WriteLine($"[typeof({attributeKey}Attribute)] = new System.Attribute[] {{");

      code.Indent++;
      GenerateLogicPropAttributeMappingEntry(attributeEntries, code);
      code.Indent--;

      code.Write("}");
      if (!isLast) { code.Write(","); }
      code.WriteLine();

      i++;
    }
  }

  public static void GenerateLogicPropAttributeMappingEntry(
    ImmutableArray<PropertyAttribute> attribute, IndentedTextWriter code
  ) {
    var i = 0;

    foreach (var entry in attribute) {
      var isLast = i == attribute.Length - 1;

      code.Write($"new {entry.Name}Attribute(");
      if (entry.ArgExpressions.Length > 0) {
        code.Write(string.Join(", ", entry.ArgExpressions));
      }
      code.Write(")");

      if (!isLast) { code.Write(","); }
      code.WriteLine();

      i++;
    }
  }

  public static void GenerateTypeRegistry(
    SourceProductionContext context,
    GenerationData data
  ) {
    var code = CreateCodeWriter();
    code.WriteLine("public partial class TypeRegistry : Chickensoft.LogicBlocks.Types.ITypeRegistry {");
    code.WriteLine();

    code.Indent++;
    CreateVisibleTypesProperty(data.VisibleTypes, code);
    CreateVisibleInstantiableTypesProperty(data.VisibleInstantiableTypes, code);
    CreateMetatypesProperty(data.Metatypes, code);
    code.Indent--;
    code.Write("}");

    context.AddSource(
      hintName: $"TypeRegistry.g.cs",
      source: code.InnerWriter.ToString()
    );
  }

  public static DeclaredTypeInfo Resolve(
    GeneratorSyntaxContext context, CancellationToken _
  ) {
    var typeDecl = (TypeDeclarationSyntax)context.Node;

    var name = typeDecl.Identifier.ValueText;
    var construction = GetConstruction(typeDecl);
    var isPartial = IsPartial(typeDecl);
    var typeParameters = GetTypeParameters(typeDecl);

    var reference = new TypeReference(
      Name: name,
      Construction: construction,
      IsPartial: isPartial,
      TypeParameters: typeParameters
    );

    var location = GetLocation(typeDecl);
    var usings = GetUsings(typeDecl);
    var kind = GetKind(typeDecl);
    var hasMetatypeAttribute = HasMetatypeAttribute(typeDecl);
    var isTopLevelAccessible = IsTopLevelAccessible(typeDecl);

    var diagnostics = new HashSet<Diagnostic>();

    if (
      hasMetatypeAttribute && (
        !isTopLevelAccessible || // Must be top-level accessible
        !isPartial || // Must be partial
        typeParameters.Length > 0 // Must be non-generic
      )
    ) {
      diagnostics.Add(
        Diagnostics.InvalidMetatype(
          typeDecl,
          name
        )
      );
    }

    var properties = GetProperties(typeDecl);

    return new DeclaredTypeInfo(
      Reference: reference,
      Location: location,
      Usings: usings,
      Kind: kind,
      HasMetatypeAttribute: hasMetatypeAttribute,
      IsTopLevelAccessible: isTopLevelAccessible,
      Properties: properties,
      Diagnostics: diagnostics.ToImmutableHashSet()
    );
  }

  private static void CreateVisibleTypesProperty(
    ImmutableDictionary<string, DeclaredTypeInfo> types, IndentedTextWriter code
  ) {
    code.WriteLine("public System.Collections.Generic.ISet<System.Type> VisibleTypes { get; } = new System.Collections.Generic.HashSet<System.Type>() {");
    code.Indent++;
    AddTypeEntries(types, code);
    code.Indent--;
    code.WriteLine("};");
    code.WriteLine();
  }

  private static void CreateVisibleInstantiableTypesProperty(
    ImmutableDictionary<string, DeclaredTypeInfo> types, IndentedTextWriter code
  ) {
    code.WriteLine("public System.Collections.Generic.IDictionary<System.Type, System.Func<object>> VisibleInstantiableTypes { get; } = new System.Collections.Generic.Dictionary<System.Type, System.Func<object>>() {");
    code.Indent++;
    AddInstantiableTypeEntries(types, code);
    code.Indent--;
    code.WriteLine("};");
    code.WriteLine();
  }

  private static void CreateMetatypesProperty(
    ImmutableDictionary<string, DeclaredTypeInfo> types, IndentedTextWriter code
  ) {
    code.WriteLine("public System.Collections.Generic.IDictionary<System.Type, Chickensoft.LogicBlocks.IMetatype> Metatypes { get; } = new System.Collections.Generic.Dictionary<System.Type, Chickensoft.LogicBlocks.IMetatype>() {");
    code.Indent++;
    AddMetatypeEntries(types, code);
    code.Indent--;
    code.WriteLine("};");
    code.WriteLine();
  }

  private static void AddTypeEntries(
    ImmutableDictionary<string, DeclaredTypeInfo> types, IndentedTextWriter code
  ) {
    var i = 0;
    foreach (var type in types.Values) {
      var typeName = type.FullName;
      var isLast = i == types.Count - 1;
      code.WriteLine($"typeof({typeName}){(isLast ? "" : ",")}");
      i++;
    }
  }

  private static void AddInstantiableTypeEntries(
    ImmutableDictionary<string, DeclaredTypeInfo> types,
    IndentedTextWriter code
  ) {
    var i = 0;

    foreach (var type in types.Values) {
      var typeName = type.FullName;
      var isLast = i == types.Count - 1;
      code.WriteLine($"[typeof({typeName})] = () => System.Activator.CreateInstance<{typeName}>(){(isLast ? "" : ",")}");
      i++;
    }
  }

  private static void AddMetatypeEntries(
    ImmutableDictionary<string, DeclaredTypeInfo> types,
    IndentedTextWriter code
  ) {
    var i = 0;
    foreach (var type in types.Values) {
      var typeName = type.FullName;
      var isLast = i == types.Count - 1;
      code.WriteLine($"[typeof({typeName})] = new {typeName}.Metatype(){(isLast ? "" : ",")}");
      i++;
    }
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

  public static Construction GetConstruction(TypeDeclarationSyntax typeDecl) {
    if (typeDecl is ClassDeclarationSyntax classDecl) {
      return classDecl.Modifiers.Any(SyntaxKind.StaticKeyword)
        ? Construction.StaticClass
        : Construction.Class;
    }
    else if (typeDecl is InterfaceDeclarationSyntax) {
      return Construction.Interface;
    }
    else if (typeDecl is RecordDeclarationSyntax recordDecl) {
      return recordDecl.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword)
        ? Construction.RecordStruct
        : Construction.RecordClass;
    }
    return Construction.Class;
  }

  public static ImmutableArray<string> GetTypeParameters(
    TypeDeclarationSyntax typeDecl
  ) =>
    typeDecl.TypeParameterList?.Parameters
      .Select(p => p.Identifier.ValueText)
      .ToImmutableArray()
      ?? ImmutableArray<string>.Empty;

  /// <summary>
  /// True if the type declaration is explicitly marked as visible at the
  /// top-level of the project.
  /// </summary>
  /// <param name="typeDecl">Type declaration syntax.</param>
  /// <returns>True if marked as `public` or `internal`.</returns>
  public static bool IsTopLevelAccessible(TypeDeclarationSyntax typeDecl) =>
    typeDecl.Modifiers.Any(m =>
      m.IsKind(SyntaxKind.PublicKeyword) ||
      m.IsKind(SyntaxKind.InternalKeyword)
    );

  public static bool IsPartial(TypeDeclarationSyntax typeDecl) =>
    typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword);

  public static bool HasMetatypeAttribute(TypeDeclarationSyntax typeDecl) =>
    typeDecl.AttributeLists.Any(
      list => list.Attributes.Any(
        attr => attr.Name.ToString() == "LogicModel"
      )
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
    var types = new LinkedList<TypeReference>();
    for (
      var parent = source.Parent; parent is not null; parent = parent.Parent
    ) {
      if (parent is BaseNamespaceDeclarationSyntax @namespace) {
        foreach (
          var namespacePart in @namespace.Name.ToString().Split('.').Reverse()
        ) {
          namespaces.AddFirst(namespacePart);
        }
      }
      else if (parent is TypeDeclarationSyntax type) {
        var typeParameters = type.TypeParameterList?.Parameters
            .Select(p => p.Identifier.ValueText)
            .ToImmutableArray()
            ?? ImmutableArray<string>.Empty;

        var construction = GetConstruction(type);
        var isPartial = IsPartial(type);

        var containingType = new TypeReference(
          Name: type.Identifier.ValueText,
          Construction: construction,
          IsPartial: isPartial,
          TypeParameters: typeParameters
        );

        types.AddFirst(containingType);
      }
    }

    return new TypeLocation(namespaces, types);
  }

  public static ImmutableHashSet<UsingDirective> GetUsings(
    TypeDeclarationSyntax type
  ) {
    var allUsings = SyntaxFactory.List<UsingDirectiveSyntax>();
    foreach (var parent in type.Ancestors(false)) {
      if (parent is BaseNamespaceDeclarationSyntax ns) {
        allUsings = allUsings.AddRange(ns.Usings);
      }
      else if (parent is CompilationUnitSyntax comp) {
        allUsings = allUsings.AddRange(comp.Usings);
      }
    }
    return allUsings
      .Select(@using => new UsingDirective(
          Alias: @using.Alias?.Name.NormalizeWhitespace().ToString(),
          TypeName: @using.Name.NormalizeWhitespace().ToString(),
          IsGlobal: @using.GlobalKeyword is { ValueText: "global" },
          IsStatic: @using.StaticKeyword is { ValueText: "static" },
          IsAlias: @using.Alias != default
        )
      )
      .ToImmutableHashSet();
  }

  public static ImmutableArray<Property> GetProperties(
    TypeDeclarationSyntax type
  ) {
    var properties = ImmutableArray.CreateBuilder<Property>();
    foreach (var property in type.Members.OfType<PropertyDeclarationSyntax>()) {
      var isPartial = property.Modifiers.Any(SyntaxKind.PartialKeyword);

      if (isPartial) { continue; } // Partial properties are unsupported.

      var propertyAttributes = property.AttributeLists
        .SelectMany(list => list.Attributes)
        .Select(attr => new PropertyAttribute(
          Name: attr.Name.NormalizeWhitespace().ToString(),
          ArgExpressions: attr.ArgumentList?.Arguments
            .Select(arg => arg.NormalizeWhitespace().ToString())
            .ToImmutableArray()
            ?? ImmutableArray<string>.Empty
        ))
        .ToImmutableArray();

      if (propertyAttributes.Length == 0) {
        // Only record information about properties marked with attributes.
        continue;
      }

      var hasSetter = property.AccessorList?.Accessors
        .Any(accessor => accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
        ?? false;

      var isNullable =
        property.Type is NullableTypeSyntax ||
        (property.Type is GenericNameSyntax generic && generic.Identifier.ValueText == "Nullable");

      properties.Add(
        new Property(
          Name: property.Identifier.ValueText,
          Type: property.Type.NormalizeWhitespace().ToString(),
          HasSetter: hasSetter,
          IsNullable: isNullable,
          Attributes: propertyAttributes
        )
      );
    }
    return properties.ToImmutable();
  }

  public static IndentedTextWriter CreateCodeWriter() =>
    new(new StringWriter(), "  ");
}
